using SqExpress.IntTest.Context;

namespace SqExpress.IntTest.Tables;

public static class AllTables
{
    public static TableBase[] BuildAllTableList(SqlDialect dialect) => new TableBase[]
    {
        GetFk0(Alias.Empty), GetItAllColumnTypes(dialect, Alias.Empty), GetFk1A(Alias.Empty), GetFk1B(Alias.Empty),
        GetItCompany(dialect, Alias.Empty), GetItUser(dialect, Alias.Empty), GetFk2AB(Alias.Empty),
        GetItCustomer(Alias.Empty),
        GetFk3AB(Alias.Empty), GetItOrder(Alias.Empty)
    };

    public static TableFk0 GetFk0(Alias alias) => new TableFk0(alias);
    public static TableFk0 GetFk0() => new TableFk0(Alias.Auto);

    public static TableItAllColumnTypes GetItAllColumnTypes(SqlDialect dialect, Alias alias)
        => new TableItAllColumnTypes(dialect, alias);

    public static TableItAllColumnTypes GetItAllColumnTypes(SqlDialect dialect)
        => new TableItAllColumnTypes(dialect, Alias.Auto);

    public static TableFk1A GetFk1A(Alias alias) => new TableFk1A(alias);
    public static TableFk1A GetFk1A() => new TableFk1A(Alias.Auto);
    public static TableFk1B GetFk1B(Alias alias) => new TableFk1B(alias);
    public static TableFk1B GetFk1B() => new TableFk1B(Alias.Auto);
    public static TableItCompany GetItCompany(SqlDialect dialect, Alias alias) => new TableItCompany(dialect, alias);
    public static TableItCompany GetItCompany(SqlDialect dialect) => new TableItCompany(dialect, Alias.Auto);
    public static TableItUser GetItUser(SqlDialect dialect, Alias alias) => new TableItUser(dialect, alias);
    public static TableItUser GetItUser(SqlDialect dialect) => new TableItUser(dialect, Alias.Auto);
    public static TableFk2AB GetFk2AB(Alias alias) => new TableFk2AB(alias);
    public static TableFk2AB GetFk2AB() => new TableFk2AB(Alias.Auto);
    public static TableItCustomer GetItCustomer(Alias alias) => new TableItCustomer(alias);
    public static TableItCustomer GetItCustomer() => new TableItCustomer(Alias.Auto);
    public static TableFk3AB GetFk3AB(Alias alias) => new TableFk3AB(alias);
    public static TableFk3AB GetFk3AB() => new TableFk3AB(Alias.Auto);
    public static TableItOrder GetItOrder(Alias alias) => new TableItOrder(alias);
    public static TableItOrder GetItOrder() => new TableItOrder(Alias.Auto);
}

public static class Helpers
{
    public static bool IsUnicode(bool value, SqlDialect dialect)
    {
        return dialect == SqlDialect.PgSql || value;
    }

    public static int? ArrayLimit(int? value, SqlDialect dialect)
    {
        return dialect == SqlDialect.PgSql ? null : value;
    }
}