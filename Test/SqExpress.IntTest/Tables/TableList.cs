
namespace SqExpress.IntTest.Tables
{
    public static class TableList
    {
        public static Company Company(Alias alias = default) => new Company(alias);

        public static User User(Alias alias = default) => new User(alias);

        public static Customer Customer(Alias alias = default) => new Customer(alias);

        public static Order Order(Alias alias = default) => new Order(alias);

        public static TableFk0 Fk0(Alias alias = default) => new TableFk0(alias);

        public static TableFk1A Fk1A(Alias alias = default) => new TableFk1A(alias);

        public static TableFk1B Fk1B(Alias alias = default) => new TableFk1B(alias);

        public static TableFk2Ab Fk2Ab(Alias alias = default) => new TableFk2Ab(alias);

        public static TableFk3Ab Fk3Ab(Alias alias = default) => new TableFk3Ab(alias);
    }
}