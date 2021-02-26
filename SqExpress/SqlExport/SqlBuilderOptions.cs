using System;
using System.Collections.Generic;
using SqExpress.Utils;

namespace SqExpress.SqlExport
{
    public class SqlBuilderOptions
    {
        public static SqlBuilderOptions Default = new SqlBuilderOptions();

        public IReadOnlyList<SchemaMap>? SchemaMap { get; private set; }

        public bool AvoidNameQuoting { get; private set; }

        private SqlBuilderOptions() : this(null, false) { }

        public SqlBuilderOptions(IReadOnlyList<SchemaMap>? schemaMap, bool avoidNameQuoting)
        {
            this.SchemaMap = schemaMap;
            this.AvoidNameQuoting = avoidNameQuoting;
        }

        public SqlBuilderOptions WithSchemaMap(IReadOnlyList<SchemaMap>? schemaMap)
        {
            var result = this.Clone();
            result.SchemaMap = schemaMap;
            return result;
        }

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

    public readonly struct SchemaMap
    {
        public readonly string From;

        public readonly string To;

        public SchemaMap(string from, string to)
        {
            this.From = from;
            this.To = to;
        }
    }
}