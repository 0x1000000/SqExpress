using System;
using System.Collections.Generic;

namespace SqExpress.SqlExport
{
    public class SqlBuilderOptions
    {
        public static SqlBuilderOptions Default = new SqlBuilderOptions();

        public IReadOnlyList<SchemaMap>? SchemaMap { get; private set; }

        private SqlBuilderOptions() : this(null) { }

        public SqlBuilderOptions(IReadOnlyList<SchemaMap>? schemaMap)
        {
            this.SchemaMap = schemaMap;
        }

        public SqlBuilderOptions WithSchemaMap(IReadOnlyList<SchemaMap>? schemaMap)
        {
            var result = this.Clone();
            result.SchemaMap = schemaMap;
            return result;
        }

        private SqlBuilderOptions Clone() 
            => new SqlBuilderOptions(schemaMap: this.SchemaMap);


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