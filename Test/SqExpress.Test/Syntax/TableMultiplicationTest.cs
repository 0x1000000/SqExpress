using NUnit.Framework;
using SqExpress.Syntax.Select;
using static SqExpress.SqQueryBuilder;

namespace SqExpress.Test.Syntax
{
    [TestFixture]
    public class TableMultiplicationTest
    {
        [Test]
        public void InnerJoinTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();
            var tCustomerOrder = Tables.CustomerOrder();


            var select = SelectOne()
                .From(tUser)
                .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                .InnerJoin(tCustomerOrder, on: tCustomerOrder.CustomerId == tCustomer.CustomerId)
                .Done();

            var (tables, on) = select.From!.ToTableMultiplication();

            Assert.AreEqual(3, tables.Count);
            Assert.AreEqual(tUser, tables[0]);
            Assert.AreEqual(tCustomer, tables[1]);
            Assert.AreEqual(tCustomerOrder, tables[2]);

            Assert.AreEqual("[A0].[UserId]=[A1].[UserId] AND [A2].[CustomerId]=[A0].[CustomerId]", on!.ToSql());
        }

        [Test]
        public void InnerCrossJoinTest()
        {
            var tUser = Tables.User();
            var tUser2 = Tables.User();
            var tCustomer = Tables.Customer();
            var tCustomerOrder = Tables.CustomerOrder();


            var select = SelectOne()
                .From(tUser)
                .InnerJoin(tUser2, on: tUser2.UserId == tUser.UserId)
                .CrossJoin(tCustomer)
                .InnerJoin(tCustomerOrder, on: tCustomerOrder.CustomerId == tCustomer.CustomerId)
                .Done();

            var (tables, on) = select.From!.ToTableMultiplication();

            Assert.AreEqual(4, tables.Count);
            Assert.AreEqual(tUser, tables[0]);
            Assert.AreEqual(tUser2, tables[1]);
            Assert.AreEqual(tCustomer, tables[2]);
            Assert.AreEqual(tCustomerOrder, tables[3]);

            Assert.AreEqual("[A0].[UserId]=[A1].[UserId] AND [A2].[CustomerId]=[A3].[CustomerId]", on!.ToSql());
        }

        [Test]
        public void CrossJoinTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();
            var tCustomerOrder = Tables.CustomerOrder();

            var select = SelectOne()
                .From(tUser)
                .CrossJoin(tCustomer)
                .CrossJoin(tCustomerOrder)
                .Done();

            var (tables, on) = select.From!.ToTableMultiplication();

            Assert.AreEqual(3, tables.Count);
            Assert.AreEqual(tUser, tables[0]);
            Assert.AreEqual(tCustomer, tables[1]);
            Assert.AreEqual(tCustomerOrder, tables[2]);

            Assert.IsNull(on);
        }


        [Test]
        public void DerivedTableTest()
        {
            var tUser = Tables.User();
            var tCustomer = Tables.Customer();
            var td = new TestDerivedTable();
            var tCustomerOrder = Tables.CustomerOrder();

            var select = SelectOne()
                .From(tUser)
                .InnerJoin(tCustomer, tCustomer.UserId == tUser.UserId)
                .CrossJoin(tCustomerOrder)
                .InnerJoin(td, td.CustomerId == tCustomer.CustomerId | td.OrderId == tCustomerOrder.OrderId)
                .Done();

            var (tables, on) = select.From!.ToTableMultiplication();

            Assert.AreEqual(4, tables.Count);
            Assert.AreEqual(tUser, tables[0]);
            Assert.AreEqual(tCustomer, tables[1]);
            Assert.AreEqual(tCustomerOrder, tables[2]);
            Assert.AreEqual("(SELECT [A0].[CustomerId],[A0].[OrderId] FROM [dbo].[CustomerOrder] [A0])[A1]", tables[3].ToSql());

            Assert.AreEqual("[A0].[UserId]=[A1].[UserId] AND([A2].[CustomerId]=[A0].[CustomerId] OR [A2].[OrderId]=[A3].[OrderId])", on!.ToSql());
        }

        [Test]
        public void DerivedTableExposesRegisteredSelectingAndUnderlyingQuery()
        {
            var td = new TestDerivedTable();

            var selecting = td.ExtractSelecting();

            Assert.AreEqual(2, selecting.Count);
            Assert.AreSame(td.CustomerId, selecting[0]);
            Assert.AreSame(td.OrderId, selecting[1]);
            Assert.AreEqual("SELECT [A0].[CustomerId],[A0].[OrderId] FROM [dbo].[CustomerOrder] [A0]", td.CreateSubQuery().ToSql());
        }

        [Test]
        public void CrossJoinedTableFallsBackToWildcardWhenOneSideHasUnknownSelecting()
        {
            var user = Tables.User();
            var openJson = TableFunctionSys("OPENJSON", Literal("[]")).As(TableAlias("J"));

            var sql = new ExprCrossedTable(user, openJson).CreateSubQuery().ToSql();

            Assert.AreEqual("SELECT * FROM [dbo].[user] [A0] CROSS JOIN OPENJSON('[]') [J]", sql);
        }

        class TestDerivedTable : DerivedTableBase
        {
            private readonly CustomerOrder _table;

            public TestDerivedTable(Alias alias = default) : base(alias)
            {
                this._table = Tables.CustomerOrder();

                this.CustomerId = this._table.CustomerId.AddToDerivedTable(this);
                this.OrderId = this._table.OrderId.AddToDerivedTable(this);
            }

            public readonly Int32CustomColumn OrderId;

            public readonly Int32CustomColumn CustomerId;

            protected override IExprSubQuery CreateQuery()
            {
                return Select(this._table.CustomerId, this._table.OrderId)
                    .From(this._table)
                    .Done();
            }
        }

        [Test]
        public void LeftJoinTest()
        {
            Assert.Throws<SqExpressException>(() =>
            {
                var tUser = Tables.User();
                var tCustomer = Tables.Customer();
                var tCustomerOrder = Tables.CustomerOrder();


                var select = SelectOne()
                    .From(tUser)
                    .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                    .LeftJoin(tCustomerOrder, on: tCustomerOrder.CustomerId == tCustomer.CustomerId)
                    .Done();

                select.From!.ToTableMultiplication();
            }, "'Left JOIN' does not support converting to 'USING' list");
        }

        [Test]
        public void FullJoinTest()
        {
            Assert.Throws<SqExpressException>(() =>
            {
                var tUser = Tables.User();
                var tCustomer = Tables.Customer();
                var tCustomerOrder = Tables.CustomerOrder();

                var select = SelectOne()
                    .From(tUser)
                    .InnerJoin(tCustomer, on: tCustomer.UserId == tUser.UserId)
                    .FullJoin(tCustomerOrder, on: tCustomerOrder.CustomerId == tCustomer.CustomerId)
                    .Done();

                select.From!.ToTableMultiplication();
            }, "'Full JOIN' does not support converting to 'USING' list");
        }
    }
}
