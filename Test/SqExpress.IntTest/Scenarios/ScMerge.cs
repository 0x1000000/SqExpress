using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;

namespace SqExpress.IntTest.Scenarios
{
    public class ScMerge : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var tt = new TestMergeTmpTable();
            await context.Database.Statement(tt.Script.Create());

            var testData = new List<TestMergeData>
            {
                new TestMergeData(1, 10),
                new TestMergeData(2, 11)
            };

            //Insert
            context.WriteLine("Inserting using MERGE..");
            await SqQueryBuilder.MergeDataInto(tt, testData)
                .MapDataKeys(TestMergeData.GetUpdateKeyMapping)
                .MapData(TestMergeData.GetUpdateMapping)
                .WhenMatchedThenUpdate()
                .AlsoSet(s=>s.Set(s.Target.Version, s.Target.Version+1))
                .WhenNotMatchedByTargetThenInsert()
                .Done()
                .Exec(context.Database);

            var dataFromDb = await SqQueryBuilder.Select(TestMergeDataRow.GetColumns(tt))
                .From(tt)
                .OrderBy(tt.Id)
                .QueryList(context.Database, r => TestMergeDataRow.Read(r, tt));

            if (RowDataToString(dataFromDb) != "1,10,0;2,11,0")
            {
                throw new Exception("Incorrect data");
            }

            context.WriteLine("Updating using MERGE..");

            testData.Add(new TestMergeData(3, 12));
            testData[0] = testData[0].WithValue(100);

            await SqQueryBuilder.MergeDataInto(tt, testData)
                .MapDataKeys(TestMergeData.GetUpdateKeyMapping)
                .MapData(TestMergeData.GetUpdateMapping)
                .WhenMatchedThenUpdate((s,t)=> tt.Value.WithSource(t) != s.Value.WithSource(s.Alias))
                .AlsoSet(s => s.Set(s.Target.Version, s.Target.Version + 1))
                .WhenNotMatchedByTargetThenInsert()
                .Done()
                .Exec(context.Database);

            dataFromDb = await SqQueryBuilder.Select(TestMergeDataRow.GetColumns(tt))
                .From(tt)
                .OrderBy(tt.Id)
                .QueryList(context.Database, r => TestMergeDataRow.Read(r, tt));

            if (RowDataToString(dataFromDb) != "1,100,1;2,11,0;3,12,0")
            {
                throw new Exception("Incorrect data");
            }


            context.WriteLine("Updating (BY SOURCE) using MERGE..");

            testData = new List<TestMergeData>
            {
                new TestMergeData(1, 17),
            };

            await SqQueryBuilder.MergeDataInto(tt, testData)
                .MapDataKeys(TestMergeData.GetUpdateKeyMapping)
                .MapData(TestMergeData.GetUpdateMapping)
                .WhenMatchedThenUpdate((s, t) => tt.Value.WithSource(t) != s.Value.WithSource(s.Alias))
                .AlsoSet(s => s.Set(s.Target.Version, s.Target.Version + 1))
                .WhenNotMatchedBySourceThenUpdate(t=> t.Value == 12).Set(s => s.Set(s.Target.Version, s.Target.Version + 10))
                .Done()
                .Exec(context.Database);

            dataFromDb = await SqQueryBuilder.Select(TestMergeDataRow.GetColumns(tt))
                .From(tt)
                .OrderBy(tt.Id)
                .QueryList(context.Database, r => TestMergeDataRow.Read(r, tt));

            if (RowDataToString(dataFromDb) != "1,17,2;2,11,0;3,12,10")
            {
                throw new Exception("Incorrect data");
            }

            context.WriteLine("Deleting (BY SOURCE) using MERGE..");

            await SqQueryBuilder.MergeDataInto(tt, testData)
                .MapDataKeys(TestMergeData.GetUpdateKeyMapping)
                .MapData(TestMergeData.GetUpdateMapping)
                .WhenMatchedThenUpdate()
                .AlsoSet(s => s.Set(s.Target.Version, s.Target.Version + 1))
                .WhenNotMatchedBySourceThenDelete(t=> t.Value == 12)
                .Done()
                .Exec(context.Database);

            dataFromDb = await SqQueryBuilder.Select(TestMergeDataRow.GetColumns(tt))
                .From(tt)
                .OrderBy(tt.Id)
                .QueryList(context.Database, r => TestMergeDataRow.Read(r, tt));

            if (RowDataToString(dataFromDb) != "1,17,3;2,11,0")
            {
                throw new Exception("Incorrect data");
            }

            context.WriteLine("Deleting ON MATCH using MERGE..");

            await SqQueryBuilder.MergeDataInto(tt, testData)
                .MapDataKeys(TestMergeData.GetUpdateKeyMapping)
                .MapData(TestMergeData.GetUpdateMapping)
                .WhenMatchedThenDelete()
                .Done()
                .Exec(context.Database);

            dataFromDb = await SqQueryBuilder.Select(TestMergeDataRow.GetColumns(tt))
                .From(tt)
                .OrderBy(tt.Id)
                .QueryList(context.Database, r => TestMergeDataRow.Read(r, tt));

            if (RowDataToString(dataFromDb) != "2,11,0")
            {
                throw new Exception("Incorrect data");
            }

            await context.Database.Statement(tt.Script.Drop());
        }

        private static string RowDataToString(IReadOnlyList<TestMergeDataRow> data)
            => string.Join(';', data.Select(d=>$"{d.Id},{d.Value},{d.Version}"));
    }
}