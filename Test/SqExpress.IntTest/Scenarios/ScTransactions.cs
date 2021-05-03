using System;
using System.Data;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScTransactions : IScenario
    {
        private readonly bool _newConnection;

        public ScTransactions(bool newConnection)
        {
            this._newConnection = newConnection;
        }

        public async Task Exec(IScenarioContext context)
        {
            ISqDatabase database = this._newConnection ? context.CreteConnection() : context.Database;

            var guid = Guid.Parse("58AD8253-4F8F-4C84-B930-4F58A8F25912");
            var tCompany = AllTables.GetItCompany();
            var data = new[] { new CompanyInitData(id: 0, name: "TestCompany", externalId: guid) };

            var exprInsert = InsertDataInto(tCompany, data)
                .MapData(CompanyInitData.GetMapping)
                .AlsoInsert(m => m
                    .Set(m.Target.Modified, GetUtcDate())
                    .Set(m.Target.Created, GetUtcDate())
                    .Set(m.Target.Version, 1)
                ).Done();

            using (database.BeginTransactionOrUseExisting(out var newTransaction))
            {
                if (!newTransaction)
                {
                    throw new Exception("New transaction should be started");
                }

                using var t2 = database.BeginTransactionOrUseExisting(out newTransaction);

                await exprInsert.Exec(database);
                t2.Rollback();

                t2.Commit();

                if (newTransaction)
                {
                    throw new Exception("Already existing transaction should be reused");
                }
                if (!await CheckExistence())
                {
                    throw new Exception("Inside transaction the data should be visible");
                }
            }

            if (await CheckExistence())
            {
                throw new Exception("Transaction was not committed, so it should return nothing");
            }

            using (var t = database.BeginTransaction())
            {
                await exprInsert.Exec(database);
                t.Commit();
            }

            if (!await CheckExistence())
            {
                throw new Exception("Transaction was committed, so it should return something");
            }

            using (var t = database.BeginTransaction(IsolationLevel.Serializable))
            {
                await Delete(tCompany).Where(tCompany.ExternalId == guid).Exec(database);
                t.Commit();
            }

            if (await CheckExistence())
            {
                throw new Exception("The row is suppose to be deleted");
            }

            if (this._newConnection)
            {
                database.Dispose();
            }

            async Task<bool> CheckExistence()
            {
                var col = await Select(Literal(1).As("Col"))
                    .From(tCompany)
                    .Where(tCompany.ExternalId == guid)
                    .QueryList(database, r => r.GetInt32("Col"));

                return col.Count > 0;
            }
        }
    }
}