using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using SqExpress.IntTest.Context;
using SqExpress.IntTest.Tables;
using SqExpress.IntTest.Tables.Derived;
using SqExpress.IntTest.Tables.Models;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.IntTest.Scenarios
{
    public class ScInsertCompanies : IScenario
    {
        public async Task Exec(IScenarioContext context)
        {
            var company = AllTables.GetItCompany();

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

            var insertedDuplicates = await InsertDataInto(company, this.ReadCompanyData().Take(2))
                .MapData(s => s
                    .Set(s.Target.ExternalId, s.Source.ExternalId)
                    .Set(s.Target.CompanyName, s.Source.Name))
                .AlsoInsert(s => s
                    .Set(s.Target.Modified, now)
                    .Set(s.Target.Created, now)
                    .Set(s.Target.Version, 1))
                .CheckExistenceBy(company.ExternalId)
                .Output(company.CompanyId)
                .QueryList(context.Database, r => company.CompanyId.Read(r));

            if (insertedDuplicates.Count > 0)
            {
                throw new Exception("CheckExistenceBy does not work");
            }

            var customer = AllTables.GetItCustomer();

            //Insert customer
            await InsertDataInto(customer, inserted)
                .MapData(s => s.Set(s.Target.CompanyId, s.Source)).Exec(context.Database);

            context.WriteLine($"{inserted.Count} have been inserted into {nameof(TableItCustomer)}");


            var tCustomerName = new CustomerName();

            var users = await Select(CustomerNameData.GetColumns(tCustomerName))
                .From(tCustomerName)
                .Where(tCustomerName.CustomerTypeId == 1)
                .OrderBy(tCustomerName.Name)
                .OffsetFetch(0, 5)
                .QueryList(context.Database, r => CustomerNameData.Read(r, tCustomerName));

            var companies = await Select(CustomerNameData.GetColumns(tCustomerName))
                .From(tCustomerName)
                .Where(tCustomerName.CustomerTypeId == 2)
                .OrderBy(tCustomerName.CustomerId)
                .OffsetFetch(0, 5)
                .QueryList(context.Database, r => CustomerNameData.Read(r, tCustomerName));

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
        }

        private IEnumerable<JsonCompanyData> ReadCompanyData()
        {
            var assembly = typeof(Program).GetTypeInfo().Assembly;

            const string resourceName = "SqExpress.IntTest.TestData.company.json";
            using Stream? resource = assembly.GetManifestResourceStream(resourceName);
            if (resource == null)
            {
                throw new Exception($"Could not find resource name \"{resourceName}\"");
            }
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
                        buffer.Name = userProperty.Value.GetString() ?? string.Empty;
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