using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.DbMetadata.Internal.Model;

namespace SqExpress.CodeGenUtil.CodeGen
{
    internal class ColumnTypeGenerator : IColumnTypeVisitor<IdentifierNameSyntax, object?>
    {
        public static readonly ColumnTypeGenerator Instance = new ColumnTypeGenerator();

        private ColumnTypeGenerator() { }

        public IdentifierNameSyntax VisitBooleanColumnType(BooleanColumnType booleanColumnType, object? arg)
        {
            string className = booleanColumnType.IsNullable ? nameof(NullableBooleanTableColumn) : nameof(BooleanTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitByteColumnType(ByteColumnType byteColumnType, object? arg)
        {
            string className = byteColumnType.IsNullable ? nameof(NullableByteTableColumn) : nameof(ByteTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitByteArrayColumnType(ByteArrayColumnType byteArrayColumnType, object? arg)
        {
            string className = byteArrayColumnType.IsNullable ? nameof(NullableByteArrayTableColumn) : nameof(ByteArrayTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitInt16ColumnType(Int16ColumnType int16ColumnType, object? arg)
        {
            string className = int16ColumnType.IsNullable ? nameof(NullableInt16TableColumn) : nameof(Int16TableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitInt32ColumnType(Int32ColumnType int32ColumnType, object? arg)
        {
            string className = int32ColumnType.IsNullable ? nameof(NullableInt32TableColumn) : nameof(Int32TableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitInt64ColumnType(Int64ColumnType int64ColumnType, object? arg)
        {
            string className = int64ColumnType.IsNullable ? nameof(NullableInt64TableColumn) : nameof(Int64TableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitDoubleColumnType(DoubleColumnType doubleColumnType, object? arg)
        {
            string className = doubleColumnType.IsNullable ? nameof(NullableDoubleTableColumn) : nameof(DoubleTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitDecimalColumnType(DecimalColumnType decimalColumnType, object? arg)
        {
            string className = decimalColumnType.IsNullable ? nameof(NullableDecimalTableColumn) : nameof(DecimalTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitDateTimeColumnType(DateTimeColumnType dateTimeColumnType, object? arg)
        {
            string className = dateTimeColumnType.IsNullable ? nameof(NullableDateTimeTableColumn) : nameof(DateTimeTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitDateTimeOffsetColumnType(DateTimeOffsetColumnType dateTimeColumnType, object? arg)
        {
            string className = dateTimeColumnType.IsNullable ? nameof(NullableDateTimeOffsetTableColumn) : nameof(DateTimeOffsetTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitStringColumnType(StringColumnType stringColumnType, object? arg)
        {
            string className = stringColumnType.IsNullable ? nameof(NullableStringTableColumn) : nameof(StringTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitGuidColumnType(GuidColumnType guidColumnType, object? arg)
        {
            string className = guidColumnType.IsNullable ? nameof(NullableGuidTableColumn) : nameof(GuidTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }

        public IdentifierNameSyntax VisitXmlColumnType(XmlColumnType xmlColumnType, object? arg)
        {
            string className = xmlColumnType.IsNullable ? nameof(NullableStringTableColumn) : nameof(StringTableColumn);

            return SyntaxFactory.IdentifierName(className);
        }
    }
}