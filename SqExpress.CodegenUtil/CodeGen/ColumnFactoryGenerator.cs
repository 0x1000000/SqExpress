using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGenUtil.Model;
using SqExpress.Syntax.Type;
using static SqExpress.CodeGenUtil.CodeGen.SyntaxHelpers;

namespace SqExpress.CodeGenUtil.CodeGen
{
    internal readonly struct ColumnContext
    {
        public readonly ColumnModel ColumnModel;

        public readonly IReadOnlyDictionary<TableRef, TableModel> Tables;

        public ColumnContext(ColumnModel columnModel, IReadOnlyDictionary<TableRef, TableModel> tables)
        {
            this.ColumnModel = columnModel;
            this.Tables = tables;
        }
    }

    internal class ColumnFactoryGenerator : TableBase, IColumnTypeVisitor<ExpressionSyntax, ColumnContext>
    {
        public static readonly ColumnFactoryGenerator Instance = new ColumnFactoryGenerator();


        public string NameOfAddIndex => nameof(AddIndex);
        public string NameOfAddUniqueIndex => nameof(AddUniqueIndex);
        public string NameOfAddClusteredIndex => nameof(AddClusteredIndex);
        public string NameOfAddUniqueClusteredIndex => nameof(AddUniqueClusteredIndex);

        public ExpressionSyntax VisitBooleanColumnType(BooleanColumnType booleanColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = booleanColumnType.IsNullable ? nameof(CreateNullableBooleanColumn) : nameof(CreateBooleanColumn);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitByteColumnType(ByteColumnType byteColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = byteColumnType.IsNullable ? nameof(CreateNullableByteColumn) : nameof(CreateByteColumn);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitByteArrayColumnType(ByteArrayColumnType byteArrayColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName;

            if (byteArrayColumnType.IsFixed)
            {
                methodName = byteArrayColumnType.IsNullable ? nameof(this.CreateNullableFixedSizeByteArrayColumn) : nameof(CreateFixedSizeByteArrayColumn);
            }
            else
            {
                methodName = byteArrayColumnType.IsNullable ? nameof(CreateNullableByteArrayColumn) : nameof(CreateByteArrayColumn);
            }

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), LiteralExpr(byteArrayColumnType.Size), GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitInt16ColumnType(Int16ColumnType int16ColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = int16ColumnType.IsNullable ? nameof(CreateNullableInt16Column) : nameof(CreateInt16Column);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitInt32ColumnType(Int32ColumnType int32ColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = int32ColumnType.IsNullable ? nameof(CreateNullableInt32Column) : nameof(CreateInt32Column);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitInt64ColumnType(Int64ColumnType int64ColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = int64ColumnType.IsNullable ? nameof(CreateNullableInt64Column) : nameof(CreateInt64Column);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitDoubleColumnType(DoubleColumnType doubleColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = doubleColumnType.IsNullable ? nameof(CreateNullableDoubleColumn) : nameof(CreateDoubleColumn);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitDecimalColumnType(DecimalColumnType decimalColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = decimalColumnType.IsNullable ? nameof(CreateNullableDecimalColumn) : nameof(CreateDecimalColumn);

            this.CreateDecimalColumn("", new DecimalPrecisionScale(precision: 1, scale: 2));

            var argumentListSyntax = ArgumentList(
                ("precision", LiteralExpr(decimalColumnType.Precision)),
                ("scale", LiteralExpr(decimalColumnType.Scale))
            );
            var newDecimal = SyntaxFactory.ObjectCreationExpression(SyntaxFactory.IdentifierName(nameof(DecimalPrecisionScale)), argumentListSyntax, null);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), newDecimal, GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitDateTimeColumnType(DateTimeColumnType dateTimeColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = dateTimeColumnType.IsNullable ? nameof(CreateNullableDateTimeColumn) : nameof(CreateDateTimeColumn);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), LiteralExpr(dateTimeColumnType.IsDate), GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitStringColumnType(StringColumnType stringColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName;

            if (stringColumnType.IsFixed)
            {
                methodName = stringColumnType.IsNullable ? nameof(this.CreateNullableFixedSizeStringColumn) : nameof(CreateFixedSizeStringColumn);
                return InvokeThis(methodName, ("name", LiteralExpr(columnModel.DbName.Name)), ("size", LiteralExpr(stringColumnType.Size)), ("isUnicode", LiteralExpr(stringColumnType.IsUnicode)), ("columnMeta", GenColumnMeta(columnContext)));
            }
            else
            {
                methodName = stringColumnType.IsNullable ? nameof(this.CreateNullableStringColumn) : nameof(CreateStringColumn);
                return InvokeThis(methodName, ("name", LiteralExpr(columnModel.DbName.Name)), ("size", LiteralExpr(stringColumnType.Size)), ("isUnicode", LiteralExpr(stringColumnType.IsUnicode)), ("isText", LiteralExpr(stringColumnType.IsText)), ("columnMeta", GenColumnMeta(columnContext)));
            }
        }

        public ExpressionSyntax VisitGuidColumnType(GuidColumnType guidColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = guidColumnType.IsNullable ? nameof(CreateNullableGuidColumn) : nameof(CreateGuidColumn);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), GenColumnMeta(columnContext));
        }

        public ExpressionSyntax VisitXmlColumnType(XmlColumnType xmlColumnType, ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;
            string methodName = xmlColumnType.IsNullable ? nameof(CreateNullableXmlColumn) : nameof(CreateXmlColumn);

            return InvokeThis(methodName, LiteralExpr(columnModel.DbName.Name), GenColumnMeta(columnContext));
        }

        private static ExpressionSyntax GenColumnMeta(ColumnContext columnContext)
        {
            ColumnModel columnModel = columnContext.ColumnModel;

            ExpressionSyntax? result = null;

            ExpressionSyntax EnsureResult(string methodName, string? g1 = null)
            {
                return string.IsNullOrEmpty(g1)
                    ? result?.MemberAccess(methodName) ??
                      SyntaxFactory.IdentifierName(nameof(ColumnMeta)).MemberAccess(methodName)
                    : result?.MemberAccessGeneric(methodName, g1) ??
                      SyntaxFactory.IdentifierName(nameof(ColumnMeta)).MemberAccessGeneric(methodName, g1);
            }

            if (columnModel.Pk.HasValue)
            {
                result = EnsureResult(nameof(ColumnMeta.PrimaryKey)).Invoke();
            }

            if (columnModel.Identity)
            {
                result = EnsureResult(nameof(ColumnMeta.Identity)).Invoke();
            }

            if (columnModel.DefaultValue != null)
            {
                result = EnsureResult(nameof(ColumnMeta.DefaultValue)).Invoke(GetDefaultArgument(columnModel.DefaultValue.Value));
            }

            if (columnModel.Fk != null)
            {
                foreach (var columnRef in columnModel.Fk)
                {
                    if(!columnContext.Tables.TryGetValue(columnRef.Table, out var tableModel))
                    {
                        throw new SqExpressCodeGenException($"Could not find table model for: \"{columnRef.Table}\"");
                    }

                    var fkColumnModel = tableModel.Columns.FirstOrDefault(c => c.DbName.Equals(columnRef));
                    if (fkColumnModel == null)
                    {
                        throw new SqExpressCodeGenException($"Could not find column model for: \"{columnRef}\"");
                    }

                    var lambda = SyntaxFactory.SimpleLambdaExpression(FuncParameter("t"), MemberAccess("t", fkColumnModel.Name));

                    result = EnsureResult(nameof(ColumnMeta.ForeignKey), tableModel.Name).Invoke(lambda);
                }
            }

            return result ?? NullLiteral();

            static ExpressionSyntax GetDefaultArgument(DefaultValue defaultValue)
            {
                ExpressionSyntax arg;
                switch (defaultValue.Type)
                {
                    case DefaultValueType.Raw:
                        var rawValue = defaultValue.RawValue ??
                                       throw new SqExpressCodeGenException("String raw value cannot be null");
                        arg = SyntaxFactory.IdentifierName(nameof(SqQueryBuilder))
                            .MemberAccess(nameof(SqQueryBuilder.UnsafeValue))
                            .Invoke(LiteralExpr(rawValue));
                        break;
                    case DefaultValueType.Null:
                        arg = SyntaxFactory.IdentifierName(nameof(SqQueryBuilder))
                            .MemberAccess(nameof(SqQueryBuilder.Null));
                        break;
                    case DefaultValueType.Integer:
                        var valueRawValue = defaultValue.RawValue ??
                                            throw new SqExpressCodeGenException("Integer raw value cannot be null");
                        if (!int.TryParse(valueRawValue, out var intLiteral))
                        {
                            throw new SqExpressCodeGenException("Integer literal has invalid format: " + valueRawValue);
                        }

                        arg = LiteralExpr(intLiteral);
                        break;
                    case DefaultValueType.String:
                        var valueStringValue = defaultValue.RawValue ??
                                               throw new SqExpressCodeGenException("Integer raw value cannot be null");
                        arg = LiteralExpr(valueStringValue);
                        break;
                    case DefaultValueType.GetUtcDate:
                        arg = SyntaxFactory.IdentifierName(nameof(SqQueryBuilder))
                            .MemberAccess(nameof(SqQueryBuilder.GetUtcDate))
                            .Invoke();
                        break;
                    default:
                        throw new SqExpressCodeGenException("Unknown default value type: " + defaultValue.Type);
                }

                return arg;
            }
        }

        private ColumnFactoryGenerator() : base("", "", SqExpress.Alias.Empty)
        {
        }
    }
}