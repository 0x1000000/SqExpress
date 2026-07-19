using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGenUtil
{
    internal sealed class TableGenerationLayout
    {
        private TableGenerationLayout(IReadOnlyDictionary<TableRef, TableGenerationLayoutEntry> entries)
        {
            this.Entries = entries;
        }

        public IReadOnlyDictionary<TableRef, TableGenerationLayoutEntry> Entries { get; }

        public static TableGenerationLayout Create(
            IReadOnlyList<TableModel> tables,
            string outputDirectory,
            string baseNamespace,
            bool splitTablesBySchema)
        {
            var schemaSegments = new Dictionary<string, string>(StringComparer.Ordinal);
            if (splitTablesBySchema)
            {
                foreach (var schemaGroup in tables.GroupBy(static t => t.DbName.Schema ?? string.Empty, StringComparer.Ordinal))
                {
                    var schema = schemaGroup.Key;
                    var segment = StringHelper.DeSnake(string.IsNullOrEmpty(schema) ? "Default" : schema);
                    var collision = schemaSegments.Where(pair =>
                        string.Equals(pair.Value, segment, StringComparison.OrdinalIgnoreCase) &&
                        !string.Equals(pair.Key, schema, StringComparison.Ordinal)).ToArray();
                    if (collision.Length > 0)
                    {
                        throw new SqExpressCodeGenException(
                            $"Schemas \"{collision[0].Key}\" and \"{schema}\" both normalize to \"{segment}\". Schema-split table generation requires unique normalized schema names.");
                    }

                    schemaSegments.Add(schema, segment);
                }
            }

            var entries = new Dictionary<TableRef, TableGenerationLayoutEntry>();
            var paths = new Dictionary<string, TableRef>(StringComparer.OrdinalIgnoreCase);
            var typeNames = new Dictionary<string, TableRef>(StringComparer.Ordinal);
            foreach (var table in tables)
            {
                var schema = table.DbName.Schema ?? string.Empty;
                var schemaSegment = splitTablesBySchema ? schemaSegments[schema] : null;
                var @namespace = schemaSegment == null
                    ? baseNamespace
                    : string.IsNullOrEmpty(baseNamespace) ? schemaSegment : baseNamespace + "." + schemaSegment;
                var directory = schemaSegment == null ? outputDirectory : Path.Combine(outputDirectory, schemaSegment);
                var filePath = Path.Combine(directory, table.Name + ".cs");
                var fullyQualifiedTypeName = string.IsNullOrEmpty(@namespace)
                    ? table.Name
                    : @namespace + "." + table.Name;

                if (paths.TryGetValue(filePath, out var pathCollision))
                {
                    var guidance = splitTablesBySchema
                        ? "Generated table file names must be unique within each schema."
                        : "Enable --split-tables-by-schema to generate tables from different schemas into separate folders.";
                    throw new SqExpressCodeGenException(
                        $"Tables {pathCollision} and {table.DbName} both map to output file \"{filePath}\". {guidance}");
                }

                if (typeNames.TryGetValue(fullyQualifiedTypeName, out var typeCollision))
                {
                    throw new SqExpressCodeGenException(
                        $"Tables {typeCollision} and {table.DbName} both map to generated type \"{fullyQualifiedTypeName}\".");
                }

                paths.Add(filePath, table.DbName);
                typeNames.Add(fullyQualifiedTypeName, table.DbName);
                entries.Add(table.DbName, new TableGenerationLayoutEntry(filePath, @namespace, schemaSegment));
            }

            return new TableGenerationLayout(entries);
        }
    }

    internal readonly struct TableGenerationLayoutEntry
    {
        public TableGenerationLayoutEntry(string filePath, string @namespace, string? schemaSegment)
        {
            this.FilePath = filePath;
            this.Namespace = @namespace;
            this.SchemaSegment = schemaSegment;
        }

        public string FilePath { get; }

        public string Namespace { get; }

        public string? SchemaSegment { get; }
    }
}
