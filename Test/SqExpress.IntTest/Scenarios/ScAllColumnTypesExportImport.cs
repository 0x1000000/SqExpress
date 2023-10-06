using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;

namespace SqExpress.IntTest.Scenarios
{
    public class ScAllColumnTypesExportImport : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var table = AllTables.GetItAllColumnTypes(context.Dialect);

            var jsonStringOriginal = await ReadAsJsonString(context, table);

            await context.Database.Exec(SqQueryBuilder.Delete(table).All());

            await Insert(context, jsonStringOriginal, table);

            var jsonStringAfter = await ReadAsJsonString(context: context, table: table);

            if (jsonStringOriginal != jsonStringAfter)
            {
                throw new Exception("Export/Import/Export was not correct");
            }
        }

        private async Task Insert(IScenarioContext context, string json, TableItAllColumnTypes table)
        {

            var doc = JsonDocument.Parse(json);

            var tableName = table.FullName.TableName;

            foreach (var obj in doc.RootElement.EnumerateObject())
            {
                if (obj.Name != tableName)
                {
                    throw new Exception($"Unexpected property: {obj.Name}");
                }

                var rowsEnumerable = obj.Value.EnumerateArray()
                    .Select(rowArray =>
                    {
                        int colIndex = 0;
                        var row = new ExprValue[table.Columns.Count];
                        foreach (var cell in rowArray.EnumerateArray())
                        {
                            row[colIndex] = table.Columns[colIndex]
                                .FromString(cell.ValueKind == JsonValueKind.Null ? null : cell.GetString());
                            colIndex++;
                        }

                        return row;
                    });

                await SqQueryBuilder
                    .IdentityInsertInto(table, table.Columns.Select(c => c.ColumnName).ToList())
                    .Values(rowsEnumerable)
                    .Exec(context.Database);
            }
        }

        private static async Task<string> ReadAsJsonString(IScenarioContext context, TableItAllColumnTypes table)
        {
            using var ms = new MemoryStream();

            using Utf8JsonWriter writer = new Utf8JsonWriter(ms);

            writer.WriteStartObject();
            writer.WriteStartArray(table.FullName.TableName);

            await SqQueryBuilder
                .Select(table.Columns)
                .From(table)
                .Query(context.Database,
                    r =>
                    {
                        writer.WriteStartArray();
                        foreach (var column in table.Columns)
                        {
                            var readAsString = column.ReadAsString(r);
                            writer.WriteStringValue(readAsString);
                        }
                        writer.WriteEndArray();
                    });

            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();

            var jsonString = Encoding.UTF8.GetString(ms.ToArray());
            return jsonString;
        }
    }
}