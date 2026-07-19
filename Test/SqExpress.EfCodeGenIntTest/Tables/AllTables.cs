using System;
using System.Collections.Generic;
using SqExpress;

namespace SqExpress.EfCodeGenIntTest.Tables
{
    public static class AllTables
    {
        public static readonly IReadOnlyList<TableBase> StaticList = Array.AsReadOnly(BuildAllTableList());
        public static TableBase[] BuildAllTableList() => new TableBase[]
        {
            GetAuditAuditLogs(Alias.Empty),
            GetCatalogCategories(Alias.Empty),
            GetSalesCustomers(Alias.Empty),
            GetCatalogProducts(Alias.Empty),
            GetSalesOrders(Alias.Empty),
            GetSalesOrderLines(Alias.Empty)
        };
        public static TableBase[] BuildAllAliasedTableList() => new TableBase[]
        {
            GetAuditAuditLogs(),
            GetCatalogCategories(),
            GetSalesCustomers(),
            GetCatalogProducts(),
            GetSalesOrders(),
            GetSalesOrderLines()
        };
        public static global::SqExpress.EfCodeGenIntTest.Tables.Audit.TableAuditLogs GetAuditAuditLogs(Alias alias) => new global::SqExpress.EfCodeGenIntTest.Tables.Audit.TableAuditLogs(alias);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Audit.TableAuditLogs GetAuditAuditLogs() => new global::SqExpress.EfCodeGenIntTest.Tables.Audit.TableAuditLogs(Alias.Auto);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Catalog.TableCategories GetCatalogCategories(Alias alias) => new global::SqExpress.EfCodeGenIntTest.Tables.Catalog.TableCategories(alias);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Catalog.TableCategories GetCatalogCategories() => new global::SqExpress.EfCodeGenIntTest.Tables.Catalog.TableCategories(Alias.Auto);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableCustomers GetSalesCustomers(Alias alias) => new global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableCustomers(alias);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableCustomers GetSalesCustomers() => new global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableCustomers(Alias.Auto);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Catalog.TableProducts GetCatalogProducts(Alias alias) => new global::SqExpress.EfCodeGenIntTest.Tables.Catalog.TableProducts(alias);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Catalog.TableProducts GetCatalogProducts() => new global::SqExpress.EfCodeGenIntTest.Tables.Catalog.TableProducts(Alias.Auto);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableOrders GetSalesOrders(Alias alias) => new global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableOrders(alias);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableOrders GetSalesOrders() => new global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableOrders(Alias.Auto);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableOrderLines GetSalesOrderLines(Alias alias) => new global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableOrderLines(alias);
        public static global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableOrderLines GetSalesOrderLines() => new global::SqExpress.EfCodeGenIntTest.Tables.Sales.TableOrderLines(Alias.Auto);
    }
}