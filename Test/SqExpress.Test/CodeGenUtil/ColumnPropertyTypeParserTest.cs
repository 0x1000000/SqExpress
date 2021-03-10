#if !NETFRAMEWORK
using System.Linq;
using NUnit.Framework;
using SqExpress.CodeGenUtil.CodeGen;

namespace SqExpress.Test.CodeGenUtil
{
    [TestFixture]
    public class ColumnPropertyTypeParserTest : IColumnPropertyTypeSwitcher<string, string>
    {
        [Test]
        public void Test()
        {
            var allTypes = typeof(TableColumn).Assembly.GetTypes().Where(t => typeof(TableColumn).IsAssignableFrom(t)).ToList();

            foreach (var type in allTypes)
            {
                if (type.Name != nameof(TableColumn))
                {
                    Assert.AreEqual("Success", ColumnPropertyTypeParser.Parse(type.Name, this, "Success"));
                }
            }
        }

        public string CaseBooleanTableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableBooleanTableColumn(string arg)
        {
            return arg;
        }

        public string CaseByteTableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableByteTableColumn(string arg)
        {
            return arg;
        }

        public string CaseByteArrayTableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableByteArrayTableColumn(string arg)
        {
            return arg;
        }

        public string CaseInt16TableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableInt16TableColumn(string arg)
        {
            return arg;
        }

        public string CaseInt32TableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableInt32TableColumn(string arg)
        {
            return arg;
        }

        public string CaseInt64TableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableInt64TableColumn(string arg)
        {
            return arg;
        }

        public string CaseDecimalTableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableDecimalTableColumn(string arg)
        {
            return arg;
        }

        public string CaseDoubleTableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableDoubleTableColumn(string arg)
        {
            return arg;
        }

        public string CaseDateTimeTableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableDateTimeTableColumn(string arg)
        {
            return arg;
        }

        public string CaseGuidTableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableGuidTableColumn(string arg)
        {
            return arg;
        }

        public string CaseStringTableColumn(string arg)
        {
            return arg;
        }

        public string CaseNullableStringTableColumn(string arg)
        {
            return arg;
        }

        public string Default(string arg)
        {
            return "Fail " + arg;
        }
    }
}
#endif
