using SqExpress.IntTest.Context;

namespace SqExpress.IntTest.Tables;

public static class AllTables
{
    public static TableBase[] BuildAllTableList(SqlDialect dialect) =>
    [
        ..ForeignKeyTables.BuildTableList(),
        GetItAllColumnTypes(dialect, Alias.Empty), GetItCompany(dialect, Alias.Empty),
        GetItUser(dialect, Alias.Empty), GetItCustomer(Alias.Empty), GetItOrder(Alias.Empty)
    ];

    public static TableItAllColumnTypes GetItAllColumnTypes(SqlDialect dialect, Alias alias)
        => new TableItAllColumnTypes(dialect, alias);

    public static TableItAllColumnTypes GetItAllColumnTypes(SqlDialect dialect)
        => new TableItAllColumnTypes(dialect, Alias.Auto);

    public static TableItCompany GetItCompany(SqlDialect dialect, Alias alias) => new TableItCompany(dialect, alias);
    public static TableItCompany GetItCompany(SqlDialect dialect) => new TableItCompany(dialect, Alias.Auto);
    public static TableItUser GetItUser(SqlDialect dialect, Alias alias) => new TableItUser(dialect, alias);
    public static TableItUser GetItUser(SqlDialect dialect) => new TableItUser(dialect, Alias.Auto);
    public static TableItCustomer GetItCustomer(Alias alias) => new TableItCustomer(alias);
    public static TableItCustomer GetItCustomer() => new TableItCustomer(Alias.Auto);
    public static TableItOrder GetItOrder(Alias alias) => new TableItOrder(alias);
    public static TableItOrder GetItOrder() => new TableItOrder(Alias.Auto);
}
