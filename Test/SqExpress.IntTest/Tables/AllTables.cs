using SqExpress;

namespace SqExpress.IntTest.Tables
{
    public static class AllTables
    {
        public static TableBase[] BuildAllTableList(bool postgres) => new TableBase[]
        {
            GetFk0(Alias.Empty), GetItAllColumnTypes(postgres, Alias.Empty), GetFk1A(Alias.Empty), GetFk1B(Alias.Empty),
            GetItCompany(Alias.Empty), GetItUser(Alias.Empty), GetFk2AB(Alias.Empty), GetItCustomer(Alias.Empty),
            GetFk3AB(Alias.Empty), GetItOrder(Alias.Empty)
        };

        public static TableFk0 GetFk0(Alias alias) => new TableFk0(alias);
        public static TableFk0 GetFk0() => new TableFk0(Alias.Auto);
        public static TableItAllColumnTypes GetItAllColumnTypes(bool postgres, Alias alias) => new TableItAllColumnTypes(postgres, alias);
        public static TableItAllColumnTypes GetItAllColumnTypes(bool postgres) => new TableItAllColumnTypes(postgres, Alias.Auto);
        public static TableFk1A GetFk1A(Alias alias) => new TableFk1A(alias);
        public static TableFk1A GetFk1A() => new TableFk1A(Alias.Auto);
        public static TableFk1B GetFk1B(Alias alias) => new TableFk1B(alias);
        public static TableFk1B GetFk1B() => new TableFk1B(Alias.Auto);
        public static TableItCompany GetItCompany(Alias alias) => new TableItCompany(alias);
        public static TableItCompany GetItCompany() => new TableItCompany(Alias.Auto);
        public static TableItUser GetItUser(Alias alias) => new TableItUser(alias);
        public static TableItUser GetItUser() => new TableItUser(Alias.Auto);
        public static TableFk2AB GetFk2AB(Alias alias) => new TableFk2AB(alias);
        public static TableFk2AB GetFk2AB() => new TableFk2AB(Alias.Auto);
        public static TableItCustomer GetItCustomer(Alias alias) => new TableItCustomer(alias);
        public static TableItCustomer GetItCustomer() => new TableItCustomer(Alias.Auto);
        public static TableFk3AB GetFk3AB(Alias alias) => new TableFk3AB(alias);
        public static TableFk3AB GetFk3AB() => new TableFk3AB(Alias.Auto);
        public static TableItOrder GetItOrder(Alias alias) => new TableItOrder(alias);
        public static TableItOrder GetItOrder() => new TableItOrder(Alias.Auto);
    }
}