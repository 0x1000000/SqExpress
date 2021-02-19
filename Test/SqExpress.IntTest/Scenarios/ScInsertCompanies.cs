using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Derived;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScInsertCompanies : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var company = Tables.TableList.Company();

            DateTime now = DateTime.UtcNow;

            var inserted = await InsertDataInto(company, this.ReadCompanyData())
                .MapData(s => s
                    .Set(s.Target.ExternalId, s.Source.ExternalId)
                    .Set(s.Target.CompanyName, s.Source.Name))
                .AlsoInsert(s => s
                    .Set(s.Target.Modified, now)
                    .Set(s.Target.Created, now)
                    .Set(s.Target.Version, 1))
                .Output(company.CompanyId)
                .QueryList(context.Database, r=> company.CompanyId.Read(r));

            var customer = Tables.TableList.Customer();

            //Insert customer
            await InsertDataInto(customer, inserted)
                .MapData(s => s.Set(s.Target.CompanyId, s.Source)).Exec(context.Database);

            context.WriteLine($"{inserted.Count} have been inserted into {nameof(Customer)}");


            var tCustomerName = new CustomerName();

            var users = await Select(tCustomerName.Columns)
                .From(tCustomerName)
                .Where(tCustomerName.CustomerTypeId == 1)
                .OrderBy(tCustomerName.Name)
                .OffsetFetch(0, 5)
                .QueryList(context.Database, r => ReadCustomerName(r, tCustomerName));

            var companies = await Select(tCustomerName.Columns)
                .From(tCustomerName)
                .Where(tCustomerName.CustomerTypeId == 2)
                .OrderBy(tCustomerName.CustomerId)
                .OffsetFetch(0, 5)
                .QueryList(context.Database, r => ReadCustomerName(r, tCustomerName));

            context.WriteLine(null);
            context.WriteLine("Top 5 Users users: ");
            context.WriteLine(null);

            foreach (var valueTuple in users)
            {
                Console.WriteLine(valueTuple);
            }
            context.WriteLine(null);
            context.WriteLine("Top 5 Users companies: ");
            context.WriteLine(null);

            foreach (var valueTuple in companies)
            {
                Console.WriteLine(valueTuple);
            }

            (int Id, string Name, short CType) ReadCustomerName(ISqDataRecordReader r, CustomerName customerName)
            {
                return (Id: customerName.CustomerId.Read(r), Name: customerName.Name.Read(r), CType: customerName.CustomerTypeId.Read(r));
            }
        }

        private IEnumerable<JsonCompanyData> ReadCompanyData()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;

            using Stream? resource = assembly.GetManifestResourceStream("SqExpress.IntTest.TestData.company.json");
            var document = JsonDocument.Parse(resource);

            foreach (var user in document.RootElement.EnumerateArray())
            {
                JsonCompanyData buffer = default;
                foreach (var userProperty in user.EnumerateObject())
                {
                    if (userProperty.Name == "external_id")
                    {
                        buffer.ExternalId = userProperty.Value.GetGuid();
                    }
                    if (userProperty.Name == "name")
                    {
                        buffer.Name = userProperty.Value.GetString();
                    }
                }
                yield return buffer;
            }
        }

        private struct JsonCompanyData
        {
            public Guid ExternalId;
            public string Name;
        }
    }
}