namespace SqExpress.IntTest.Tables
{
    public static class TableList
    {
        public static Company Company(Alias alias = default) => new Company(alias);

        public static User User(Alias alias = default) => new User(alias);

        public static Customer Customer(Alias alias = default) => new Customer(alias);

        public static Order Order(Alias alias = default) => new Order(alias);
    }
}