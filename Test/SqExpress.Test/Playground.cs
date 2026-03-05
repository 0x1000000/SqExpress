using System;
using NUnit.Framework;
using System.Threading.Tasks;
using SqExpress.DataAccess;
using SqExpress.SqlParser;

namespace SqExpress.Test;

public class Playground
{
    [Test]
    public void Test()
    {
        var t = new TableUsers();

        var sql = SqTSqlParser.Parse("SELECT * FROM Users WHERE UserId = @userId", [t]).WithParams(("userId", 1)).ToSql();

        Console.WriteLine(sql);
    }

    public static IExprQuery Build(out TableUsers u)
    {
        u = new TableUsers("u");
        var query = SqQueryBuilder.Select(u.UserId, u.Name.As("UserName"))
            .From(u)
            .Where(u.IsActive == 1)
            .OrderBy(SqQueryBuilder.Desc(u.Name))
            .Done();

        return query;
    }

    public static async Task Query(ISqDatabase database)
    {
        await foreach (var r in Build(out var u).Query(database))
        {
            var userId = u.UserId.Read(r);
            var userName = u.Name.Read(r, "UserName");
        }
    }

    public sealed class TableUsers : TableBase
    {
        public BooleanTableColumn IsActive { get; }
        public Int32TableColumn Name { get; }
        public Int32TableColumn UserId { get; }

        public TableUsers(Alias alias = default) : base("dbo", "Users", alias)
        {
            this.IsActive = CreateBooleanColumn("IsActive");
            this.Name = CreateInt32Column("Name");
            this.UserId = CreateInt32Column("UserId");
        }
    }
}
