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
            GetAuditLogs(Alias.Empty),
            GetCategories(Alias.Empty),
            GetCustomers(Alias.Empty),
            GetProducts(Alias.Empty),
            GetOrders(Alias.Empty),
            GetOrderLines(Alias.Empty)
        };
        public static TableBase[] BuildAllAliasedTableList() => new TableBase[]
        {
            GetAuditLogs(),
            GetCategories(),
            GetCustomers(),
            GetProducts(),
            GetOrders(),
            GetOrderLines()
        };
        public static TableAuditLogs GetAuditLogs(Alias alias) => new TableAuditLogs(alias);
        public static TableAuditLogs GetAuditLogs() => new TableAuditLogs(Alias.Auto);
        public static TableCategories GetCategories(Alias alias) => new TableCategories(alias);
        public static TableCategories GetCategories() => new TableCategories(Alias.Auto);
        public static TableCustomers GetCustomers(Alias alias) => new TableCustomers(alias);
        public static TableCustomers GetCustomers() => new TableCustomers(Alias.Auto);
        public static TableProducts GetProducts(Alias alias) => new TableProducts(alias);
        public static TableProducts GetProducts() => new TableProducts(Alias.Auto);
        public static TableOrders GetOrders(Alias alias) => new TableOrders(alias);
        public static TableOrders GetOrders() => new TableOrders(Alias.Auto);
        public static TableOrderLines GetOrderLines(Alias alias) => new TableOrderLines(alias);
        public static TableOrderLines GetOrderLines() => new TableOrderLines(Alias.Auto);
    }
}