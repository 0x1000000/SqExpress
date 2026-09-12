using System;
using System.Linq;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios;

public sealed class ScGetTablesComplex : IScenario
{
    public async Task Exec(IScenarioContext context)
    {
        if (context.Dialect == SqlDialect.Sqlite)
        {
            //Known issue: SQLite does not support Outer Apply for JSON_TABLE, so we skip this test for SQLite.
            return;
        }

        var rows = JsonTable(
                """
                [
                  {
                    "id": 1,
                    "name": "Alice",
                    "active": true,
                    "address": {
                      "city": "Toronto",
                      "postalCode": "M5V"
                    },
                    "roles": ["admin", "editor"]
                  },
                  {
                    "id": 2,
                    "name": "Bob",
                    "active": false,
                    "address": {
                      "city": "Montreal",
                      "postalCode": "H2X"
                    },
                    "roles": ["viewer"]
                  }
                ]
                """,
                "$")
            .Value("Id", "$.id", SqlType.Int32)
            .Value("Name", "$.name", SqlType.String())
            .Value("Active", "$.active", SqlType.Boolean)
            .Value("City", "$.address.city", SqlType.String())
            .Value("PostalCode", "$.address.postalCode", SqlType.String())
            .Query("Roles", "$.roles")
            .Ordinal("Index")
            .As("rows");

        var roles = TableAlias("roles");

        var query = Select(
                rows.Column("Index"),
                rows.Column("Id"),
                rows.Column("Name"),
                rows.Column("Active"),
                rows.Column("City"),
                rows.Column("PostalCode"),
                roles.Column("Role"))
            .From(rows)
            .OuterApply(JsonTable(rows.Column("Roles"), "$").Value("Role", "$", SqlType.String()).As(roles))
            .Done();

        var result = await query.QueryList(context.Database, r=> new
        {
            Index = r.GetInt32("Index"),
            Id = r.GetInt32("Id"),
            Name = r.GetString("Name"),
            Active = r.GetBoolean("Active"),
            City = r.GetString("City"),
            PostalCode = r.GetString("PostalCode"),
            Role = r.GetString("Role")
        });

        var actual = result
            .OrderBy(i => i.Index)
            .ThenBy(i => i.Role, StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            new { Index = 0, Id = 1, Name = "Alice", Active = true, City = "Toronto", PostalCode = "M5V", Role = "admin" },
            new { Index = 0, Id = 1, Name = "Alice", Active = true, City = "Toronto", PostalCode = "M5V", Role = "editor" },
            new { Index = 1, Id = 2, Name = "Bob", Active = false, City = "Montreal", PostalCode = "H2X", Role = "viewer" }
        };

        if (!actual.SequenceEqual(expected))
        {
            throw new Exception(
                "Complex JSON table expansion returned unexpected rows." + Environment.NewLine +
                "Expected: " + string.Join(", ", expected.Select(i => $"({i.Index}, {i.Id}, {i.Name}, {i.Active}, {i.City}, {i.PostalCode}, {i.Role})")) + Environment.NewLine +
                "Actual: " + string.Join(", ", actual.Select(i => $"({i.Index}, {i.Id}, {i.Name}, {i.Active}, {i.City}, {i.PostalCode}, {i.Role})")));
        }
    }
}
