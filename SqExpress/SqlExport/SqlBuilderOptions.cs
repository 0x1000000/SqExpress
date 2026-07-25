using System;
using System.Collections.Generic;

namespace SqExpress.SqlExport
{
    /// <summary>
    /// Configures identifier rendering and schema-name mapping performed by SQL exporters.
    /// </summary>
    /// <remarks>
    /// Instances are treated as value-like configuration objects. The <c>With...</c> methods return
    /// a modified copy and leave the original instance unchanged.
    /// </remarks>
    public class SqlBuilderOptions
    {
        /// <summary>
        /// Gets the shared options instance that uses normal identifier quoting and no schema mapping.
        /// </summary>
        public static SqlBuilderOptions Default = new SqlBuilderOptions();

        /// <summary>
        /// Gets the ordered schema mappings applied while SQL is rendered, or <see langword="null"/> when schemas are unchanged.
        /// </summary>
        public IReadOnlyList<SchemaMap>? SchemaMap { get; private set; }

        /// <summary>
        /// Gets a value indicating whether identifier delimiters should be omitted.
        /// </summary>
        /// <remarks>
        /// Enable this only when all identifiers are safe for the target dialect. Disabling quoting can make
        /// reserved words or identifiers containing special characters invalid.
        /// </remarks>
        public bool AvoidNameQuoting { get; private set; }

        private SqlBuilderOptions() : this(null, false) { }

        /// <summary>
        /// Creates exporter options with the specified schema mappings and identifier-quoting behavior.
        /// </summary>
        /// <param name="schemaMap">Schema-name substitutions, or <see langword="null"/> for none.</param>
        /// <param name="avoidNameQuoting"><see langword="true"/> to omit dialect-specific identifier delimiters.</param>
        public SqlBuilderOptions(IReadOnlyList<SchemaMap>? schemaMap, bool avoidNameQuoting)
        {
            this.SchemaMap = schemaMap;
            this.AvoidNameQuoting = avoidNameQuoting;
        }

        /// <summary>
        /// Returns a copy that uses the specified schema mappings.
        /// </summary>
        /// <param name="schemaMap">Schema-name substitutions, or <see langword="null"/> to disable mapping.</param>
        /// <returns>A new options instance.</returns>
        public SqlBuilderOptions WithSchemaMap(IReadOnlyList<SchemaMap>? schemaMap)
        {
            var result = this.Clone();
            result.SchemaMap = schemaMap;
            return result;
        }

        /// <summary>
        /// Returns a copy with the requested identifier-quoting behavior.
        /// </summary>
        /// <param name="avoidQuoteName"><see langword="true"/> to omit identifier delimiters.</param>
        /// <returns>A new options instance.</returns>
        public SqlBuilderOptions WithAvoidQuoteName(bool avoidQuoteName)
        {
            var result = this.Clone();
            result.AvoidNameQuoting = avoidQuoteName;
            return result;
        }

        private SqlBuilderOptions Clone()
            => new SqlBuilderOptions(schemaMap: this.SchemaMap, avoidNameQuoting: this.AvoidNameQuoting);

        internal string MapSchema(string schemaName)
        {
            if (this.SchemaMap != null)
            {
                for (int i = 0; i < this.SchemaMap.Count; i++)
                {
                    var map = this.SchemaMap[i];

                    if (string.Equals(map.From, schemaName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        schemaName = map.To;
                        break;
                    }
                }
            }

            return schemaName;
        }
    }

    /// <summary>
    /// Describes a schema-name substitution applied during SQL export.
    /// </summary>
    public readonly struct SchemaMap
    {
        /// <summary>
        /// The source schema name to match, without regard to case.
        /// </summary>
        public readonly string From;

        /// <summary>
        /// The schema name to emit when <see cref="From"/> is matched.
        /// </summary>
        public readonly string To;

        /// <summary>
        /// Creates a schema-name substitution.
        /// </summary>
        /// <param name="from">The schema name used by the expression tree.</param>
        /// <param name="to">The schema name to emit.</param>
        public SchemaMap(string from, string to)
        {
            this.From = from;
            this.To = to;
        }
    }
}
