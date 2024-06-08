using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Transactions;
using SqExpress.DataAccess;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using static SqExpress.SqQueryBuilder;
using IsolationLevel = System.Data.IsolationLevel;

namespace SqExpress.IntTest.Scenarios;

public class ScTransactionsDeadlock : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        await using var connection1 = context.CreteConnection();
        await using var connection2 = context.CreteConnection();

        await using var transaction1 = await this.CreateTransaction(connection1);
        await using var transaction2 = await this.CreateTransaction(connection2);

        var tCompany = AllTables.GetItCompany(context.Dialect);
        var tUser = AllTables.GetItUser(context.Dialect);

        Exception? exception = null;

        try
        {
            await Update(tCompany).Set(tCompany.Version, tCompany.Version + 1).All().Exec(connection1);
            await Update(tUser).Set(tUser.Version, tUser.Version + 1).All().Exec(connection2);

            var t1 = Update(tUser).Set(tUser.Version, tUser.Version + 1).All().Exec(connection1);
            var t2 = Update(tCompany).Set(tCompany.Version, tCompany.Version + 1).All().Exec(connection2);

            await Task.WhenAll(t1, t2);

        }
        catch (SqDatabaseCommandException e)
        {
            exception = e;
        }

        if (exception == null)
        {
            throw new Exception("Deadlock exception was expected");
        }

        Console.WriteLine(exception.InnerException!.GetType().Name);


        await transaction1.RollbackAsync();
        await transaction2.RollbackAsync();
    }


    private async ValueTask<ISqTransaction> CreateTransaction(ISqDatabase database)
    {
        var t2Task = database.BeginTransactionAsync();
        var errorMessage = string.Empty;
        try
        {
            await database.BeginTransactionAsync();
        }
        catch (SqExpressException e)
        {
            errorMessage = e.Message;
        }

        if (errorMessage != "There is an already running transaction associated with this connection")
        {
            throw new Exception("The error was expected");
        }


        return await t2Task;
    }
}
