using System;
using System.Data;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Models;

namespace SqExpress.IntTest.Scenarios;

public class ScTransactionsAsync : IScenario
{
    private readonly bool _newConnection;

    public ScTransactionsAsync(bool newConnection)
    {
        this._newConnection = newConnection;
    }

    public async Task Exec(IScenarioContext context)
    {
        ISqDatabase database = this._newConnection ? context.CreteConnection() : context.Database;

        var guid = Guid.Parse("58AD8253-4F8F-4C84-B930-4F58A8F25912");
        var tCompany = AllTables.GetItCompany(context.Dialect);
        var data = new[] { new CompanyInitData(id: 0, name: "TestCompany", externalId: guid) };

        var exprInsert = SqQueryBuilder.InsertDataInto(tCompany, data)
            .MapData(CompanyInitData.GetMapping)
            .AlsoInsert(m => m
                .Set(m.Target.Modified, SqQueryBuilder.GetUtcDate())
                .Set(m.Target.Created, SqQueryBuilder.GetUtcDate())
                .Set(m.Target.Version, 1)
            ).Done();

        var (t1, newTransaction) = await database.BeginTransactionOrUseExistingAsync();
        await using (t1)
        {
            if (!newTransaction)
            {
                throw new Exception("New transaction should be started");
            }

            (var t2, newTransaction) = await database.BeginTransactionOrUseExistingAsync();

            await using (t2)
            {
                await exprInsert.Exec(database);
                await t2.RollbackAsync();

                await t2.CommitAsync();
            }

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

        await using (var t = database.BeginTransaction())
        {
            await exprInsert.Exec(database);
            await t.CommitAsync();
        }

        if (!await CheckExistence())
        {
            throw new Exception("Transaction was committed, so it should return something");
        }

        await using (var t = await database.BeginTransactionAsync(IsolationLevel.Serializable))
        {
            await SqQueryBuilder.Delete(tCompany).Where(tCompany.ExternalId == guid).Exec(database);
            await t.CommitAsync();
        }

        if (await CheckExistence())
        {
            throw new Exception("The row is suppose to be deleted");
        }

        if (this._newConnection)
        {
            await database.DisposeAsync();
        }

        async Task<bool> CheckExistence()
        {
            var col = await SqQueryBuilder.Select(SqQueryBuilder.Literal(1).As("Col"))
                .From(tCompany)
                .Where(tCompany.ExternalId == guid)
                .QueryList(database, r => r.GetInt32("Col"));

            return col.Count > 0;
        }
    }
}
