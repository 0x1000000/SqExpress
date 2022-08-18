using System.Threading;
using NUnit.Framework;

namespace SqExpress.Test.Meta
{
    [TestFixture]
    public class TableFkRefTest
    {
        [Test]
        public void SelfFkTest()
        {
            var table = new SelfFkTable();

            Assert.AreEqual(table.Id.ColumnName, table.RefId.ColumnMeta?.ForeignKeyColumns?[0].ColumnName);
        }

        [Test]
        public void CrossFkTest()
        {
            var table1 = new CrossFkTable1();
            var table2 = new CrossFkTable2();

            Assert.AreEqual(table2.Id2.ColumnName, table1.RefId.ColumnMeta?.ForeignKeyColumns?[0].ColumnName);
            Assert.AreEqual(table1.Id1.ColumnName, table2.RefId.ColumnMeta?.ForeignKeyColumns?[0].ColumnName);
        }

        [Test]
        public void MultiThreadTest()
        {
            var t1 = new Thread(Body);
            var t2 = new Thread(Body);

            t1.Start();
            t2.Start();
            Assert.IsTrue(t1.Join(1000));
            Assert.IsTrue(t2.Join(1000));

            void Body()
            {
                for (int i = 0; i < 10000; i++)
                {
                    new CrossFkTable1();
                    new CrossFkTable2();
                }
            }
        }

        class SelfFkTable : TableBase
        {
            public SelfFkTable() : base("dbo", "SelfFkTable")
            {
                this.Id = this.CreateInt32Column("Id");
                this.RefId = this.CreateInt32Column("RefId", ColumnMeta.ForeignKey<SelfFkTable>(t => t.Id));
            }

            public Int32TableColumn RefId { get; }

            public Int32TableColumn Id { get; }
        }

        class CrossFkTable1 : TableBase
        {
            public CrossFkTable1() : base("dbo", "CrossFkTable1")
            {
                this.Id1 = this.CreateInt32Column("Id1");
                this.RefId = this.CreateInt32Column("RefId", ColumnMeta.ForeignKey<CrossFkTable2>(t => t.Id2));
            }

            public Int32TableColumn RefId { get; }

            public Int32TableColumn Id1 { get; }
        }

        class CrossFkTable2 : TableBase
        {
            public CrossFkTable2() : base("dbo", "CrossFkTable2")
            {
                this.Id2 = this.CreateInt32Column("Id2");
                this.RefId = this.CreateInt32Column("RefId", ColumnMeta.ForeignKey<CrossFkTable1>(t => t.Id1));
            }

            public Int32TableColumn RefId { get; }

            public Int32TableColumn Id2 { get; }
        }
    }
}