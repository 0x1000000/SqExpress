using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using SqExpress.DataAccess;
using SqExpress.SqlExport;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.GetStarted
{
    class Program
    {
        static async Task Main()
        {
            try
            {
                await RunMsSql();
                await RunPostgresSql();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task RunMsSql()
        {
            int commandCounter = 0;

            DbCommand SqlCommandFactory(SqlConnection connection, string sqlText)
            {
                Console.WriteLine($"Command #{++commandCounter}");
                Console.WriteLine(sqlText);
                Console.WriteLine();
                return new SqlCommand(sqlText, connection);
            }

            using (var connection = new SqlConnection("Data Source = (local); Initial Catalog = TestDatabase; Integrated Security = True"))
            {
                using (var database = new SqDatabase<SqlConnection>(
                    connection: connection,
                    commandFactory: SqlCommandFactory,
                    sqlExporter: TSqlExporter.Default))
                {
                    await Script(database, false);
                }
            }
        }

        private static async Task RunPostgresSql()
        {
            int commandCounter = 0;

            DbCommand NpgsqlCommandFactory(NpgsqlConnection connection, string sqlText)
            {
                Console.WriteLine($"Command #{++commandCounter}");
                Console.WriteLine(sqlText);
                Console.WriteLine();
                return new NpgsqlCommand(sqlText, connection);
            }

            using (var connection = new NpgsqlConnection("Host=localhost;Port=5432;Username=postgres;Password=test;Database=test"))
            {
                using (var database = new SqDatabase<NpgsqlConnection>(
                    connection: connection,
                    commandFactory: NpgsqlCommandFactory,
                    sqlExporter: new PgSqlExporter(builderOptions: SqlBuilderOptions.Default
                        .WithSchemaMap(schemaMap: new[] {new SchemaMap(@from: "dbo", to: "public")}))))
                {
                    await Script(database: database, true);
                }
            }
        }

        private static async Task Script(ISqDatabase database, bool postgres)
        {
            await Step1CreatingTables(database);
            await Step2InsertingData(database);
            await Step3SelectingData(database);
            await Step4UpdatingData(database);
            await Step5DeletingData(database);
            await Step6CreatingOrganizations(database);
            await Step7CreatingCustomers(database);
            await Step8JoinTables(database);
            await Step9UseDerivedTables(database);
            if (!postgres)
            {
                await Step10Merge(database);
            }
        }

        private static async Task Step1CreatingTables(ISqDatabase database)
        {
            var tables = new TableBase[]{ new TableUser() , new TableCompany(), new TableCustomer() };

            foreach (var table in tables.Reverse())
            {
                await database.Statement(table.Script.DropIfExist());
            }
            foreach (var table in tables)
            {
                await database.Statement(table.Script.Create());
            }
        }

        private static async Task Step2InsertingData(ISqDatabase database)
        {
            var tUser = new TableUser();

            var data = new[]
            {
                new {FirstName = "Francois", LastName = "Sturman"},
                new {FirstName = "Allina", LastName = "Freeborne"},
                new {FirstName = "Maye", LastName = "Maloy"},
            };

            await InsertDataInto(tUser, data)
                .MapData(s => s
                    .Set(s.Target.FirstName, s.Source.FirstName)
                    .Set(s.Target.LastName, s.Source.LastName))
                .AlsoInsert(s => s
                    .Set(s.Target.Version, 1)
                    .Set(s.Target.ModifiedAt, GetUtcDate()))
                .Exec(database);
        }

        private static async Task Step3SelectingData(ISqDatabase database)
        {
            var tUser = new TableUser();

            var selectResult = await Select(tUser.UserId, tUser.FirstName, tUser.LastName)
                .From(tUser)
                .OrderBy(tUser.FirstName, tUser.LastName)
                .QueryList(database,
                    r => (
                        Id: tUser.UserId.Read(r),
                        FirstName: tUser.FirstName.Read(r),
                        LastName: tUser.LastName.Read(r)));

            foreach (var record in selectResult)
            {
                Console.WriteLine(record);
            }
        }

        private static async Task Step4UpdatingData(ISqDatabase database)
        {
            var tUser = new TableUser();

            await Update(tUser)
                .Set(tUser.LastName, "Malloy")
                .Set(tUser.Version, tUser.Version+1)
                .Set(tUser.ModifiedAt, GetUtcDate())
                .Where(tUser.LastName == "Maloy")
                .Exec(database);

            //Writing to console without storing in memory
            await Select(tUser.Columns)
                .From(tUser)
                .Query(database, (object)null, (agg, record)=>
                {
                    Console.Write(tUser.UserId.Read(record) + ",");
                    Console.Write(tUser.FirstName.Read(record) + " ");
                    Console.Write(tUser.LastName.Read(record) + ",");
                    Console.Write(tUser.Version.Read(record) + ",");
                    Console.WriteLine(tUser.ModifiedAt.Read(record).ToString("s"));
                    return agg;
                });
        }

        private static async Task Step5DeletingData(ISqDatabase database)
        {
            var tUser = new TableUser();

            await Delete(tUser)
                .Where(tUser.FirstName.Like("May%"))
                .Output(tUser.UserId)
                .Query(database, (object)null, (agg, record)=>
                {
                    Console.WriteLine("Removed user id: " + tUser.UserId.Read(record));
                    return agg;
                });
        }

        private static async Task Step6CreatingOrganizations(ISqDatabase database)
        {
            var tCompany = new TableCompany();

            Console.WriteLine("Companies:");
            await InsertDataInto(tCompany, new[] {"Microsoft", "Google"})
                .MapData(s => s.Set(s.Target.CompanyName, s.Source))
                .AlsoInsert(s => s
                    .Set(s.Target.Version, 1)
                    .Set(s.Target.ModifiedAt, GetUtcDate()))
                .Output(tCompany.CompanyId, tCompany.CompanyName)
                .Query(database, (object) null, (agg,r) =>
                {
                    Console.WriteLine($"Id: {tCompany.CompanyId.Read(r)}, Name: {tCompany.CompanyName.Read(r)}");
                    return null;
                });
        }

        private static async Task Step7CreatingCustomers(ISqDatabase database)
        {
            var tUser = new TableUser();
            var tCompany = new TableCompany();
            var tCustomer = new TableCustomer();
            var tSubCustomer = new TableCustomer();

            //Users
            await InsertInto(tCustomer, tCustomer.UserId)
                .From(
                    Select(tUser.UserId)
                        .From(tUser)
                        .Where(!Exists(
                            SelectOne()
                                .From(tSubCustomer)
                                .Where(tSubCustomer.UserId == tUser.UserId))))
                .Exec(database);

            //Companies
            await InsertInto(tCustomer, tCustomer.CompanyId)
                .From(
                    Select(tCompany.CompanyId)
                        .From(tCompany)
                        .Where(!Exists(
                            SelectOne()
                                .From(tSubCustomer)
                                .Where(tSubCustomer.CompanyId == tCompany.CompanyId))))
                .Exec(database);

        }

        private static async Task Step8JoinTables(ISqDatabase database)
        {
            var tUser = new TableUser();
            var tCompany = new TableCompany();
            var tCustomer = new TableCustomer();

            var cType = CustomColumnFactory.Int16("Type");
            var cName = CustomColumnFactory.String("Name");

            var customers = await Select(
                    tCustomer.CustomerId,
                    Case()
                        .When(IsNotNull(tUser.UserId))
                        .Then(Cast(Literal(1), SqlType.Int16))
                        .When(IsNotNull(tCompany.CompanyId))
                        .Then(Cast(Literal(2), SqlType.Int16))
                        .Else(Null)
                        .As(cType),
                    Case()
                        .When(IsNotNull(tUser.UserId))
                        .Then(tUser.FirstName + " " + tUser.LastName)
                        .When(IsNotNull(tCompany.CompanyId))
                        .Then(tCompany.CompanyName)
                        .Else(Null)
                        .As(cName)
                )
                .From(tCustomer)
                .LeftJoin(tUser, on: tUser.UserId == tCustomer.UserId)
                .LeftJoin(tCompany, on: tCompany.CompanyId == tCustomer.CompanyId)
                .QueryList(database,
                    r => (Id: tCustomer.CustomerId.Read(r), CustomerType: cType.Read(r), Name: cName.Read(r)));

            foreach (var customer in customers)
            {
                Console.WriteLine($"Id: {customer.Id}, Name: {customer.Name}, Type: {customer.CustomerType}");
            }
        }

        private static async Task Step9UseDerivedTables(ISqDatabase database)
        {
            var tCustomer = new DerivedTableCustomer("CUST");

            var customers = await Select(tCustomer.Columns)
                .From(tCustomer)
                .Where(tCustomer.Type == 2 | tCustomer.Name.Like("%Free%"))
                .OrderBy(Desc(tCustomer.Name))
                .OffsetFetch(1, 2)
                .QueryList(database,
                    r => (Id: tCustomer.CustomerId.Read(r), CustomerType: tCustomer.Type.Read(r), Name: tCustomer.Name.Read(r)));

            foreach (var customer in customers)
            {
                Console.WriteLine($"Id: {customer.Id}, Name: {customer.Name}, Type: {customer.CustomerType}");
            }
        }

        private static async Task Step10Merge(ISqDatabase database)
        {
            var data = new[]
            {
                new {FirstName = "Francois", LastName = "Sturman2"},
                new {FirstName = "Allina", LastName = "Freeborne2"},
                new {FirstName = "Maye", LastName = "Malloy"},
            };

            var action = CustomColumnFactory.String("Actions");
            var inserted = CustomColumnFactory.NullableInt32("Inserted");
            var deleted = CustomColumnFactory.NullableInt32("Deleted");

            var tableUser = new TableUser();
            await MergeDataInto(tableUser, data)
                .MapDataKeys(s => s
                    .Set(s.Target.FirstName, s.Source.FirstName))
                .MapData(s => s
                    .Set(s.Target.LastName, s.Source.LastName))
                .WhenMatchedThenUpdate()
                .AlsoSet(s => s
                    .Set(s.Target.Version, s.Target.Version + 1)
                    .Set(s.Target.ModifiedAt, GetUtcDate()))
                .WhenNotMatchedByTargetThenInsert()
                .AlsoInsert(s => s
                    .Set(s.Target.Version, 1)
                    .Set(s.Target.ModifiedAt, GetUtcDate()))
                .Output((t, s, m) => m.Inserted(t.UserId.As(inserted)).Deleted(t.UserId.As(deleted)).Action(action))
                .Done()
                .Query(database,
                    (object) null,
                    (agg, r) =>
                    {
                        Console.WriteLine($"UserId Inserted: {inserted.Read(r)},UserId Deleted: {deleted.Read(r)} , Action: {action.Read(r)}");
                        return agg;
                    });
        }
    }
}
