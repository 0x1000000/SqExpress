using System;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using MySql.Data.MySqlClient;
using Npgsql;
using SqExpress.DataAccess;
using SqExpress.GetStarted.FavoriteFilters;
using SqExpress.SqlExport;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.SyntaxTreeOperations;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.GetStarted
{
    class Program
    {
        static async Task Main()
        {
            Console.WriteLine("SqExpress - get started");
            try
            {
                await RunMsSql("Data Source = (local); Initial Catalog = TestDatabase; Integrated Security = True");
                await RunPostgreSql("Host=localhost;Port=5432;Username=postgres;Password=test;Database=test");
                await RunMySql("server=127.0.0.1;uid=test;pwd=test;database=test");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task RunMsSql(string connectionString)
        {
            Console.WriteLine("Running on MsSQL database...");
            int commandCounter = 0;

            DbCommand SqlCommandFactory(SqlConnection connection, string sqlText)
            {
                Console.WriteLine($"Command #{++commandCounter}");
                Console.WriteLine(sqlText);
                Console.WriteLine();
                return new SqlCommand(sqlText, connection);
            }

            using (var connection = new SqlConnection(connectionString))
            {

                if (!await CheckConnection(connection, "MsSQL"))
                {
                    return;
                }
                await connection.OpenAsync();

                using (var database = new SqDatabase<SqlConnection>(
                    connection: connection,
                    commandFactory: SqlCommandFactory,
                    sqlExporter: TSqlExporter.Default))
                {
                    await Script(database, false);
                }
            }
        }

        private static async Task RunPostgreSql(string connectionString)
        {
            Console.WriteLine("Running on PostgreSQL database...");

            int commandCounter = 0;

            DbCommand NpgsqlCommandFactory(NpgsqlConnection connection, string sqlText)
            {
                Console.WriteLine($"Command #{++commandCounter}");
                Console.WriteLine(sqlText);
                Console.WriteLine();
                return new NpgsqlCommand(sqlText, connection);
            }

            using (var connection = new NpgsqlConnection(connectionString))
            {
                if (!await CheckConnection(connection, "PostgreSQL"))
                {  
                    return;
                }

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

        private static async Task RunMySql(string connectionString)
        {
            Console.WriteLine("Running on MySQL database...");
            int commandCounter = 0;

            DbCommand MySqlCommandFactory(MySqlConnection connection, string sqlText)
            {
                Console.WriteLine($"Command #{++commandCounter}");
                Console.WriteLine(sqlText);
                Console.WriteLine();
                return new MySqlCommand(sqlText, connection);
            }

            using (var connection = new MySqlConnection(connectionString))
            {
                if (!await CheckConnection(connection, "MySQL"))
                {
                    return;
                }

                using (var database = new SqDatabase<MySqlConnection>(
                    connection: connection,
                    commandFactory: MySqlCommandFactory,
                    sqlExporter: new MySqlExporter(builderOptions: SqlBuilderOptions.Default)))
                {
                    await Script(database: database, true);
                }
            }
        }

        private static async Task<bool> CheckConnection(DbConnection connection, string dbName)
        {
            try
            {
                await connection.OpenAsync();
                await connection.CloseAsync();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not open {dbName} database ({e.Message}). Check that the connection string is correct \"{connection.ConnectionString}\".");
                return false;
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
            await Step10UseDerivedTables(database);
            await Step11SubQueries(database);
            await Step11AnalyticAndWindowFunctions(database);
            await Step9SetOperations(database);
            if (!postgres)
            {
                await Step12Merge(database);
            }
            await Step13TempTables(database);
            await Step14TreeExploring(database);
            await Step15SyntaxModification(database);
            await Step16ExportToXml(database);
            await Step17ExportToPlain(database);
        }

        private static async Task Step1CreatingTables(ISqDatabase database)
        {
            var tables = new TableBase[]
            {
                new TableUser(), 
                new TableCompany(), 
                new TableCustomer(),
                new TableFavoriteFilter(),
                new TableFavoriteFilterItem()
            };

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
                .Set(tUser.Version, tUser.Version + 1)
                .Set(tUser.ModifiedAt, GetUtcDate())
                .Where(tUser.LastName == "Maloy")
                .Exec(database);

            //Writing to console without storing in memory
            await Select(tUser.Columns)
                .From(tUser)
                .Query(database,
                    record =>
                    {
                        Console.Write(tUser.UserId.Read(record) + ",");
                        Console.Write(tUser.FirstName.Read(record) + " ");
                        Console.Write(tUser.LastName.Read(record) + ",");
                        Console.Write(tUser.Version.Read(record) + ",");
                        Console.WriteLine(tUser.ModifiedAt.Read(record).ToString("s"));
                    });
        }

        private static async Task Step5DeletingData(ISqDatabase database)
        {
            var tUser = new TableUser();

            await Delete(tUser)
                .Where(tUser.FirstName.Like("May%"))
                .Output(tUser.UserId)
                .Query(database, record => Console.WriteLine("Removed user id: " + tUser.UserId.Read(record)));
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
                .Query(database,
                    r => Console.WriteLine($"Id: {tCompany.CompanyId.Read(r)}, Name: {tCompany.CompanyName.Read(r)}"));
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

        private static async Task Step9SetOperations(ISqDatabase database)
        {
            var select1 = Select(1);
            var select2 = Select(2);

            var result = await select1
                .Union(select2)
                .UnionAll(select2)
                .Except(select2)
                .Intersect(select1.Union(select2))
                .QueryList(database, r => r.GetInt32(0));

            Console.WriteLine("Result Of Set Operators:");
            Console.WriteLine(result[0]);
        }

        private static async Task Step10UseDerivedTables(ISqDatabase database)
        {
            var tCustomer = new DerivedTableCustomer("CUST");

            var customers = await Select(tCustomer.Columns)
                .From(tCustomer)
                .Where(tCustomer.Type == 2 | tCustomer.Name.Like("%Free%"))
                .OrderBy(Desc(tCustomer.Name))
                .OffsetFetch(1, 2)
                .QueryList(database,
                    r => (Id: tCustomer.CustomerId.Read(r), CustomerType: tCustomer.Type.Read(r),
                        Name: tCustomer.Name.Read(r)));

            foreach (var customer in customers)
            {
                Console.WriteLine($"Id: {customer.Id}, Name: {customer.Name}, Type: {customer.CustomerType}");
            }
        }

        private static async Task Step11SubQueries(ISqDatabase database)
        {
            var num = CustomColumnFactory.Int32("3");
            //Note: "3" (the first value) is for compatibility with MySql
            //which does not properly support values constructors

            var sum = CustomColumnFactory.Int32("Sum");

            var numbers = Values(3, 1, 1, 7, 3, 7, 3, 7, 7, 8).AsColumns(num);
            var numbersSubQuery = TableAlias();

            var mostFrequentNum = (int) await
                SelectTop(1, numbersSubQuery.Column(num))
                    .From(
                        Select(numbers.Column(num), CountOne().As(sum))
                            .From(numbers)
                            .GroupBy(numbers.Column(num))
                            .As(numbersSubQuery)
                    )
                    .OrderBy(Desc(numbersSubQuery.Column(sum)))
                    .QueryScalar(database);

            Console.WriteLine("The most frequent number: "  + mostFrequentNum);
        }

        private static async Task Step11AnalyticAndWindowFunctions(ISqDatabase database)
        {
            var cUserName = CustomColumnFactory.String("Name");
            var cNum = CustomColumnFactory.Int64("Num");
            var cFirst = CustomColumnFactory.String("First");
            var cLast = CustomColumnFactory.String("Last");

            var user = new TableUser();

            await Select(
                    (user.FirstName + " " + user.LastName)
                    .As(cUserName),
                    RowNumber()
                        /*.OverPartitionBy(some fields)*/
                        .OverOrderBy(user.FirstName)
                        .As(cNum),
                    FirstValue(user.FirstName + " " + user.LastName)
                        /*.OverPartitionBy(some fields)*/
                        .OverOrderBy(user.FirstName)
                        .FrameClauseEmpty()
                        .As(cFirst),
                    LastValue(user.FirstName + " " + user.LastName)
                        /*.OverPartitionBy(some fields)*/
                        .OverOrderBy(user.FirstName)
                        .FrameClause(
                            FrameBorder.UnboundedPreceding,
                            FrameBorder.UnboundedFollowing)
                        .As(cLast))
                .From(user)
                .Query(database,
                    r => Console.WriteLine(
                        $"Num: {cNum.Read(r)}, Name: {cUserName.Read(r)}, " +
                        $"First: {cFirst.Read(r)}, Last: {cLast.Read(r)}"));
        }

        private static async Task Step12Merge(ISqDatabase database)
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
                    r => Console.WriteLine(
                        $"UserId Inserted: {inserted.Read(r)},UserId Deleted: {deleted.Read(r)} , Action: {action.Read(r)}"));
        }

        private static async Task Step13TempTables(ISqDatabase database)
        {
            var tmp = new TempTable();

            var tableUser = new TableUser();
            var tableCompany = new TableCompany();

            await database.Statement(tmp.Script.Create());

            //Users
            await InsertInto(tmp, tmp.Name)
                .From(Select(tableUser.FirstName + " " + tableUser.LastName)
                    .From(tableUser))
                .Exec(database);

            //Companies
            await InsertInto(tmp, tmp.Name)
                .From(Select(tableCompany.CompanyName)
                    .From(tableCompany))
                .Exec(database);

            await Select(tmp.Columns)
                .From(tmp)
                .OrderBy(tmp.Name)
                .Query(database,
                    r => Console.WriteLine($"Id: {tmp.Id.Read(r)}, Name: {tmp.Name.Read(r)}"));

            //Dropping the temp table is optional
            //It will be automatically removed when
            //the connection is closed
            await database.Statement(tmp.Script.Drop());
        }

        private static async Task Step14TreeExploring(ISqDatabase database)
        {
            //Var some external filter..
            ExprBoolean filter = CustomColumnFactory.Int16("Type") == 2 /*Company*/;

            var tableCustomer = new TableCustomer();

            var baseSelect = Select(tableCustomer.CustomerId)
                .From(tableCustomer)
                .Where(filter)
                .Done();

            //Checking that filter has "Type" column
            var hasVirtualColumn = filter.SyntaxTree()
                .FirstOrDefault<ExprColumnName>(e => e.Name == "Type") != null;

            if (hasVirtualColumn)
            {
                baseSelect = (ExprQuerySpecification) baseSelect.SyntaxTree()
                    .Modify(e =>
                    {
                        var result = e;
                        //Joining with the sub query
                        if (e is TableCustomer table)
                        {
                            var derivedTable = new DerivedTableCustomer();

                            result = new ExprJoinedTable(
                                table,
                                ExprJoinedTable.ExprJoinType.Inner,
                                derivedTable,
                                table.CustomerId == derivedTable.CustomerId);
                        }

                        return result;
                    });
            }

            await baseSelect!
                .Query(database,
                    r => Console.WriteLine($"Id: {tableCustomer.CustomerId.Read(r)}"));
        }

        private static async Task Step15SyntaxModification(ISqDatabase database)
        {
            var tUser = new TableUser();

            Console.WriteLine("Original expression:");
            var expression = SelectTop(1, tUser.FirstName).From(tUser).Done();

            await expression.QueryScalar(database);

            expression = expression
                .WithTop(null)
                .WithSelectList(tUser.UserId, tUser.FirstName + " " + tUser.LastName)
                .WithWhere(tUser.UserId == 7);

            Console.WriteLine("With changed selection list  and filter:");
            await expression.QueryScalar(database);

            var tCustomer = new TableCustomer();
            expression = expression
                .WithInnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId);

            Console.WriteLine("With joined table");
            await expression.QueryScalar(database);
        }

        private static async Task Step16ExportToXml(ISqDatabase database)
        {
            var tableUser = new TableUser(Alias.Empty);

            var selectExpr = Select(tableUser.FirstName, tableUser.LastName)
                .From(tableUser)
                .Where(tableUser.LastName == "Sturman")
                .Done();

            //Exporting
            var stringBuilder = new StringBuilder();
            using XmlWriter writer = XmlWriter.Create(stringBuilder);
            selectExpr.SyntaxTree().ExportToXml(writer);

            //Importing
            XmlDocument document = new XmlDocument();
            document.LoadXml(stringBuilder.ToString());
            var restored = (ExprQuerySpecification)ExprDeserializer
                .DeserializeFormXml(document.DocumentElement!);

            var result = await restored
                .QueryList(database, r => (tableUser.FirstName.Read(r), tableUser.LastName.Read(r)));

            foreach (var name in result)
            {
                Console.WriteLine(name);
            }
        }

        private static async Task Step17ExportToPlain(ISqDatabase database)
        {
            var tableUser = new TableUser(Alias.Empty);

            ExprBoolean filter1 = tableUser.LastName == "Sturman";
            ExprBoolean filter2 = tableUser.LastName == "Freeborne";

            var tableFavoriteFilter = new TableFavoriteFilter();
            var tableFavoriteFilterItem = new TableFavoriteFilterItem();

            var filterIds = await InsertDataInto(tableFavoriteFilter, new[] {"Filter 1", "Filter 2"})
                .MapData(s => s.Set(s.Target.Name, s.Source))
                .Output(tableFavoriteFilter.FavoriteFilterId)
                .QueryList(database, r => tableFavoriteFilterItem.FavoriteFilterId.Read(r));

            var filter1Items = 
                filter1.SyntaxTree().ExportToPlainList((i, id, index, b, s, value) =>
                FilterPlainItem.Create(filterIds[0], i, id, index, b, s, value));

            var filter2Items = 
                filter2.SyntaxTree().ExportToPlainList((i, id, index, b, s, value) =>
                FilterPlainItem.Create(filterIds[1], i, id, index, b, s, value));

            await InsertDataInto(tableFavoriteFilterItem, filter1Items.Concat(filter2Items))
                .MapData(s => s
                    .Set(s.Target.FavoriteFilterId, s.Source.FavoriteFilterId)
                    .Set(s.Target.Id, s.Source.Id)
                    .Set(s.Target.ParentId, s.Source.ParentId)
                    .Set(s.Target.IsTypeTag, s.Source.IsTypeTag)
                    .Set(s.Target.ArrayIndex, s.Source.ArrayIndex)
                    .Set(s.Target.Tag, s.Source.Tag)
                    .Set(s.Target.Value, s.Source.Value)
                )
                .Exec(database);

            //Restoring
            var restoredFilterItems = await Select(tableFavoriteFilterItem.Columns)
                .From(tableFavoriteFilterItem)
                .Where(tableFavoriteFilterItem.FavoriteFilterId.In(filterIds))
                .QueryList(
                    database,
                    r => new FilterPlainItem(
                    favoriteFilterId: tableFavoriteFilterItem.FavoriteFilterId.Read(r),
                    id: tableFavoriteFilterItem.Id.Read(r),
                    parentId: tableFavoriteFilterItem.ParentId.Read(r),
                    isTypeTag: tableFavoriteFilterItem.IsTypeTag.Read(r),
                    arrayIndex: tableFavoriteFilterItem.ArrayIndex.Read(r),
                    tag: tableFavoriteFilterItem.Tag.Read(r),
                    value: tableFavoriteFilterItem.Value.Read(r)));

            var restoredFilter1 = (ExprBoolean)ExprDeserializer
                .DeserializeFormPlainList(restoredFilterItems.Where(fi =>
                    fi.FavoriteFilterId == filterIds[0]));

            var restoredFilter2 = (ExprBoolean)ExprDeserializer
                .DeserializeFormPlainList(restoredFilterItems.Where(fi =>
                    fi.FavoriteFilterId == filterIds[1]));

            Console.WriteLine("Filter 1");
            await Select(tableUser.FirstName, tableUser.LastName)
                .From(tableUser)
                .Where(restoredFilter1)
                .Query(database,
                    (object) null,
                    (s, r) =>
                    {
                        Console.WriteLine($"{tableUser.FirstName.Read(r)} {tableUser.LastName.Read(r)}");
                        return s;
                    });

            Console.WriteLine("Filter 2");
            await Select(tableUser.FirstName, tableUser.LastName)
                .From(tableUser)
                .Where(restoredFilter2)
                .Query(database,
                    (object) null,
                    (s, r) =>
                    {
                        Console.WriteLine($"{tableUser.FirstName.Read(r)} {tableUser.LastName.Read(r)}");
                        return s;
                    });
        }
    }

    public class TempTable : TempTableBase
    {
        public TempTable(Alias alias = default) : base("tempTable", alias)
        {
            this.Id = CreateInt32Column(nameof(Id), ColumnMeta.PrimaryKey().Identity());
            this.Name = CreateStringColumn(nameof(Name), 255);
        }

        public readonly Int32TableColumn Id;

        public readonly StringTableColumn Name;
    }
}
