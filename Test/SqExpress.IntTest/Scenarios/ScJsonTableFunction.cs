using System;
using System.Text;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Type;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public class ScJsonTableFunction : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        var t = new JsonColTable();
        var json = TableAlias();
        var jsonSub = TableAlias();

        await context.Database.Statement(t.Script.Create());

        await InsertDataInto(
                t,
                new[] { (id: 1, data: "{\"array\": [10,12,14,15]}"), (id: 2, data: "{\"array\": [20,22,24,25]}") }
            )
            .MapData(m => m.Set(m.Target.Key, m.Source.id).Set(m.Target.Data, m.Source.data))
            .Exec(context.Database);

        if (context.Dialect == SqlDialect.TSql)
        {
            var result = await Select(t.Key, Cast(jsonSub.Column("value"), SqlType.Int32).As("value"))
                .From(t)
                .CrossApply(
                    TableFunctionSys("OPENJSON", t.Data).As(json)
                )
                .CrossApply(
                    TableFunctionSys("OPENJSON", json.Column("value")).As(jsonSub)
                )
                .Where(json.Column("type") == 4)
                .OrderBy(Asc(t.Key), Asc(jsonSub.Column("value")))
                .Query(
                    context.Database,
                    new StringBuilder(),
                    (acc, reader) => acc.AppendFormat($"{t.Key.Read(reader)};{reader.GetInt32("value")},")
                );


            const string expected = "1;10,1;12,1;14,1;15,2;20,2;22,2;24,2;25,";
            if (result.ToString() != expected)
            {
                throw new Exception($"{result} does not equal {expected}");
            }
        }

        if (context.Dialect == SqlDialect.PgSql)
        {
            var jArrElem = TableAlias();

            var result = await Select(t.Key, Cast(jArrElem.Column("value"), SqlType.Int32))
                .From(t)
                .CrossApply(TableFunctionSys("json_array_elements_text", ScalarFunctionSys("json_extract_path", UnsafeValue($"{t.Data.ToSql(context.SqlExporter)}::json"), "array")).As(jArrElem))
                .OrderBy(Asc(t.Key), Asc(jArrElem.Column("value")))
                .Query(
                    context.Database,
                    new StringBuilder(),
                    (acc, reader) => acc.AppendFormat($"{t.Key.Read(reader)};{reader.GetInt32("value")},")
                );

            const string expected = "1;10,1;12,1;14,1;15,2;20,2;22,2;24,2;25,";
            if (result.ToString() != expected)
            {
                throw new Exception($"{result} does not equal {expected}");
            }
        }

        await context.Database.Statement(t.Script.Drop());
    }

    class JsonColTable : TempTableBase
    {
        public Int32TableColumn Key { get; }

        public StringTableColumn Data { get; }

        public JsonColTable(Alias alias = default) : base(nameof(JsonColTable), alias)
        {
            this.Key = this.CreateInt32Column("key");
            this.Data = this.CreateStringColumn("Data", 1000, isUnicode: true);
        }
    }
}
