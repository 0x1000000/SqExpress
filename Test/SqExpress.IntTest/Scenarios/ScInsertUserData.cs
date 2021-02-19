using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScInsertUserData : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var data = this.ReadUserData();

            var utcNow = DateTime.UtcNow;
            var userTable = Tables.TableList.User();

            var ids = await InsertDataInto(userTable, data)
                .MapData(s => s
                    .Set(s.Target.ExternalId, s.Source.ExternalId)
                    .Set(s.Target.FirstName, s.Source.FirstName)
                    .Set(s.Target.LastName, s.Source.LastName)
                    .Set(s.Target.Email, s.Source.Email)
                )
                .AlsoInsert(s => s
                    .Set(s.Target.RegDate, utcNow)
                    .Set(s.Target.Version, 1)
                    .Set(s.Target.Created, utcNow)
                    .Set(s.Target.Modified, utcNow)
                )
                .Output(userTable.UserId, userTable.FirstName, userTable.LastName)
                .QueryList(context.Database, r => $"{userTable.UserId.Read(r)}, {userTable.FirstName.Read(r)} {userTable.LastName.Read(r)}");

            foreach (var id in ids.Take(3))
            {
                context.WriteLine(id);    
            }
            context.WriteLine("...");
            context.WriteLine($"Total users inserted: {ids.Count}");

            var count = (long)await Select(Cast(CountOne(), SqlType.Int64)).From(userTable).QueryScalar(context.Database);
            context.WriteLine($"Users count: {count}");

            await InsertCustomers(context);
        }

        private static async Task InsertCustomers(IScenarioContext context)
        {
            var userTable = Tables.TableList.User();
            var customerTable = Tables.TableList.Customer();

            await InsertInto(customerTable, customerTable.UserId)
                .From(Select(userTable.UserId)
                    .From(userTable)
                    .Where(!Exists(SelectOne()
                        .From(customerTable)
                        .Where(customerTable.UserId == userTable.UserId))))
                .Exec(context.Database);

            context.WriteLine("Customers inserted:");

            var clCount = CustomColumnFactory.Int64("Count");

            var res = await SelectDistinct(customerTable.CustomerId, userTable.UserId, Cast(CountOneOver(), SqlType.Int64).As(clCount))
                .From(customerTable)
                .InnerJoin(userTable, @on: customerTable.UserId == userTable.UserId)
                .OrderBy(userTable.UserId)
                .OffsetFetch(0, 5)
                .QueryList(context.Database, r=> (UserId: userTable.UserId.Read(r), CustomerId: customerTable.CustomerId.Read(r), Count: clCount.Read(r)));

            foreach (var tuple in res)
            {
                Console.WriteLine(tuple);
            }
        }

        private IEnumerable<JsonUserData> ReadUserData()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;

            using Stream? resource = assembly.GetManifestResourceStream("SqExpress.IntTest.TestData.users.json");
            var document = JsonDocument.Parse(resource);

            foreach (var user in document.RootElement.EnumerateArray())
            {
                JsonUserData buffer = default;
                foreach (var userProperty in user.EnumerateObject())
                {
                    if (userProperty.Name == "external_id")
                    {
                        buffer.ExternalId = userProperty.Value.GetGuid();
                    }
                    if (userProperty.Name == "first_name")
                    {
                        buffer.FirstName = userProperty.Value.GetString();
                    }
                    if (userProperty.Name == "last_name")
                    {
                        buffer.LastName = userProperty.Value.GetString();
                    }
                    if (userProperty.Name == "email")
                    {
                        buffer.Email = userProperty.Value.GetString();
                    }

                }
                yield return buffer;
            }
        }

        private struct JsonUserData
        {
            public Guid ExternalId;
            public string FirstName;
            public string LastName;
            public string Email;
        }
    }
}