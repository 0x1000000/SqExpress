using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using SqExpress.Syntax;
using SqExpress.Syntax.Boolean;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Output;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.SyntaxTreeOperations.ExportImport;
using SqExpress.SyntaxTreeOperations.ExportImport.Internal;

namespace SqExpress.SyntaxTreeOperations
{
    public class ExprDeserializer
    {
        public static IExpr DeserializeFormPlainList(IEnumerable<IPlainItem> list)
        {
            var reader = ExprPlainReader.Create(list, out var root);
            return Deserialize(root, reader);
        }

        public static IExpr DeserializeFormXml(XmlElement node)
        {
            return Deserialize(node, ExprXmlReader.Instance);
        }

        public static IExpr Deserialize<TNode>(TNode rootElement, IExprReader<TNode> reader)
        {
            var typeTag = reader.GetNodeTypeTag(rootElement);

            switch (typeTag)
            {
                //CodeGenStart
                case "AggregateFunction": return new ExprAggregateFunction(name: GetSubNode<TNode, ExprFunctionName>(rootElement, reader, "Name"), expression: GetSubNode<TNode, ExprValue>(rootElement, reader, "Expression"), isDistinct: ReadBoolean(rootElement, reader, "IsDistinct"));
                case "Alias": return new ExprAlias(name: ReadString(rootElement, reader, "Name"));
                case "AliasGuid": return new ExprAliasGuid(id: ReadGuid(rootElement, reader, "Id"));
                case "AliasedColumn": return new ExprAliasedColumn(column: GetSubNode<TNode, ExprColumn>(rootElement, reader, "Column"), alias: GetNullableSubNode<TNode, ExprColumnAlias>(rootElement, reader, "Alias"));
                case "AliasedColumnName": return new ExprAliasedColumnName(column: GetSubNode<TNode, ExprColumnName>(rootElement, reader, "Column"), alias: GetNullableSubNode<TNode, ExprColumnAlias>(rootElement, reader, "Alias"));
                case "AliasedSelecting": return new ExprAliasedSelecting(value: GetSubNode<TNode, IExprSelecting>(rootElement, reader, "Value"), alias: GetSubNode<TNode, ExprColumnAlias>(rootElement, reader, "Alias"));
                case "AllColumns": return new ExprAllColumns(source: GetNullableSubNode<TNode, IExprColumnSource>(rootElement, reader, "Source"));
                case "AnalyticFunction": return new ExprAnalyticFunction(name: GetSubNode<TNode, ExprFunctionName>(rootElement, reader, "Name"), arguments: GetNullableSubNodeList<TNode, ExprValue>(rootElement, reader, "Arguments"), over: GetSubNode<TNode, ExprOver>(rootElement, reader, "Over"));
                case "BoolLiteral": return new ExprBoolLiteral(value: ReadNullableBoolean(rootElement, reader, "Value"));
                case "BooleanAnd": return new ExprBooleanAnd(left: GetSubNode<TNode, ExprBoolean>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprBoolean>(rootElement, reader, "Right"));
                case "BooleanEq": return new ExprBooleanEq(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "BooleanGt": return new ExprBooleanGt(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "BooleanGtEq": return new ExprBooleanGtEq(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "BooleanLt": return new ExprBooleanLt(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "BooleanLtEq": return new ExprBooleanLtEq(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "BooleanNot": return new ExprBooleanNot(expr: GetSubNode<TNode, ExprBoolean>(rootElement, reader, "Expr"));
                case "BooleanNotEq": return new ExprBooleanNotEq(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "BooleanOr": return new ExprBooleanOr(left: GetSubNode<TNode, ExprBoolean>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprBoolean>(rootElement, reader, "Right"));
                case "ByteArrayLiteral": return new ExprByteArrayLiteral(value: ReadNullableByteList(rootElement, reader, "Value"));
                case "ByteLiteral": return new ExprByteLiteral(value: ReadNullableByte(rootElement, reader, "Value"));
                case "Case": return new ExprCase(cases: GetSubNodeList<TNode, ExprCaseWhenThen>(rootElement, reader, "Cases"), defaultValue: GetSubNode<TNode, ExprValue>(rootElement, reader, "DefaultValue"));
                case "CaseWhenThen": return new ExprCaseWhenThen(condition: GetSubNode<TNode, ExprBoolean>(rootElement, reader, "Condition"), value: GetSubNode<TNode, ExprValue>(rootElement, reader, "Value"));
                case "Cast": return new ExprCast(expression: GetSubNode<TNode, IExprSelecting>(rootElement, reader, "Expression"), sqlType: GetSubNode<TNode, ExprType>(rootElement, reader, "SqlType"));
                case "Column": return new ExprColumn(source: GetNullableSubNode<TNode, IExprColumnSource>(rootElement, reader, "Source"), columnName: GetSubNode<TNode, ExprColumnName>(rootElement, reader, "ColumnName"));
                case "ColumnAlias": return new ExprColumnAlias(name: ReadString(rootElement, reader, "Name"));
                case "ColumnName": return new ExprColumnName(name: ReadString(rootElement, reader, "Name"));
                case "ColumnSetClause": return new ExprColumnSetClause(column: GetSubNode<TNode, ExprColumn>(rootElement, reader, "Column"), value: GetSubNode<TNode, IExprAssigning>(rootElement, reader, "Value"));
                case "CrossedTable": return new ExprCrossedTable(left: GetSubNode<TNode, IExprTableSource>(rootElement, reader, "Left"), right: GetSubNode<TNode, IExprTableSource>(rootElement, reader, "Right"));
                case "CurrentRowFrameBorder": return ExprCurrentRowFrameBorder.Instance;
                case "DatabaseName": return new ExprDatabaseName(name: ReadString(rootElement, reader, "Name"));
                case "DateAdd": return new ExprDateAdd(date: GetSubNode<TNode, ExprValue>(rootElement, reader, "Date"), datePart: ReadDateAddDatePart(rootElement, reader, "DatePart"), number: ReadInt32(rootElement, reader, "Number"));
                case "DateTimeLiteral": return new ExprDateTimeLiteral(value: ReadNullableDateTime(rootElement, reader, "Value"));
                case "DbSchema": return new ExprDbSchema(database: GetNullableSubNode<TNode, ExprDatabaseName>(rootElement, reader, "Database"), schema: GetSubNode<TNode, ExprSchemaName>(rootElement, reader, "Schema"));
                case "DecimalLiteral": return new ExprDecimalLiteral(value: ReadNullableDecimal(rootElement, reader, "Value"));
                case "Default": return ExprDefault.Instance;
                case "Delete": return new ExprDelete(target: GetSubNode<TNode, ExprTable>(rootElement, reader, "Target"), source: GetNullableSubNode<TNode, IExprTableSource>(rootElement, reader, "Source"), filter: GetNullableSubNode<TNode, ExprBoolean>(rootElement, reader, "Filter"));
                case "DeleteOutput": return new ExprDeleteOutput(delete: GetSubNode<TNode, ExprDelete>(rootElement, reader, "Delete"), outputColumns: GetSubNodeList<TNode, ExprAliasedColumn>(rootElement, reader, "OutputColumns"));
                case "DerivedTableQuery": return new ExprDerivedTableQuery(query: GetSubNode<TNode, IExprSubQuery>(rootElement, reader, "Query"), alias: GetSubNode<TNode, ExprTableAlias>(rootElement, reader, "Alias"), columns: GetNullableSubNodeList<TNode, ExprColumnName>(rootElement, reader, "Columns"));
                case "DerivedTableValues": return new ExprDerivedTableValues(values: GetSubNode<TNode, ExprTableValueConstructor>(rootElement, reader, "Values"), alias: GetSubNode<TNode, ExprTableAlias>(rootElement, reader, "Alias"), columns: GetSubNodeList<TNode, ExprColumnName>(rootElement, reader, "Columns"));
                case "Div": return new ExprDiv(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "DoubleLiteral": return new ExprDoubleLiteral(value: ReadNullableDouble(rootElement, reader, "Value"));
                case "Exists": return new ExprExists(subQuery: GetSubNode<TNode, IExprSubQuery>(rootElement, reader, "SubQuery"));
                case "ExprMergeNotMatchedInsert": return new ExprExprMergeNotMatchedInsert(and: GetNullableSubNode<TNode, ExprBoolean>(rootElement, reader, "And"), columns: GetSubNodeList<TNode, ExprColumnName>(rootElement, reader, "Columns"), values: GetSubNodeList<TNode, IExprAssigning>(rootElement, reader, "Values"));
                case "ExprMergeNotMatchedInsertDefault": return new ExprExprMergeNotMatchedInsertDefault(and: GetNullableSubNode<TNode, ExprBoolean>(rootElement, reader, "And"));
                case "FrameClause": return new ExprFrameClause(start: GetSubNode<TNode, ExprFrameBorder>(rootElement, reader, "Start"), end: GetNullableSubNode<TNode, ExprFrameBorder>(rootElement, reader, "End"));
                case "FuncCoalesce": return new ExprFuncCoalesce(test: GetSubNode<TNode, ExprValue>(rootElement, reader, "Test"), alts: GetSubNodeList<TNode, ExprValue>(rootElement, reader, "Alts"));
                case "FuncIsNull": return new ExprFuncIsNull(test: GetSubNode<TNode, ExprValue>(rootElement, reader, "Test"), alt: GetSubNode<TNode, ExprValue>(rootElement, reader, "Alt"));
                case "FunctionName": return new ExprFunctionName(builtIn: ReadBoolean(rootElement, reader, "BuiltIn"), name: ReadString(rootElement, reader, "Name"));
                case "GetDate": return ExprGetDate.Instance;
                case "GetUtcDate": return ExprGetUtcDate.Instance;
                case "GuidLiteral": return new ExprGuidLiteral(value: ReadNullableGuid(rootElement, reader, "Value"));
                case "InSubQuery": return new ExprInSubQuery(testExpression: GetSubNode<TNode, ExprValue>(rootElement, reader, "TestExpression"), subQuery: GetSubNode<TNode, IExprSubQuery>(rootElement, reader, "SubQuery"));
                case "InValues": return new ExprInValues(testExpression: GetSubNode<TNode, ExprValue>(rootElement, reader, "TestExpression"), items: GetSubNodeList<TNode, ExprValue>(rootElement, reader, "Items"));
                case "Insert": return new ExprInsert(target: GetSubNode<TNode, IExprTableFullName>(rootElement, reader, "Target"), targetColumns: GetNullableSubNodeList<TNode, ExprColumnName>(rootElement, reader, "TargetColumns"), source: GetSubNode<TNode, IExprInsertSource>(rootElement, reader, "Source"));
                case "InsertOutput": return new ExprInsertOutput(insert: GetSubNode<TNode, ExprInsert>(rootElement, reader, "Insert"), outputColumns: GetSubNodeList<TNode, ExprAliasedColumnName>(rootElement, reader, "OutputColumns"));
                case "InsertQuery": return new ExprInsertQuery(query: GetSubNode<TNode, IExprQuery>(rootElement, reader, "Query"));
                case "InsertValueRow": return new ExprInsertValueRow(items: GetSubNodeList<TNode, IExprAssigning>(rootElement, reader, "Items"));
                case "InsertValues": return new ExprInsertValues(items: GetSubNodeList<TNode, ExprInsertValueRow>(rootElement, reader, "Items"));
                case "Int16Literal": return new ExprInt16Literal(value: ReadNullableInt16(rootElement, reader, "Value"));
                case "Int32Literal": return new ExprInt32Literal(value: ReadNullableInt32(rootElement, reader, "Value"));
                case "Int64Literal": return new ExprInt64Literal(value: ReadNullableInt64(rootElement, reader, "Value"));
                case "IsNull": return new ExprIsNull(test: GetSubNode<TNode, ExprValue>(rootElement, reader, "Test"), not: ReadBoolean(rootElement, reader, "Not"));
                case "JoinedTable": return new ExprJoinedTable(left: GetSubNode<TNode, IExprTableSource>(rootElement, reader, "Left"), right: GetSubNode<TNode, IExprTableSource>(rootElement, reader, "Right"), searchCondition: GetSubNode<TNode, ExprBoolean>(rootElement, reader, "SearchCondition"), joinType: ReadExprJoinType(rootElement, reader, "JoinType"));
                case "Like": return new ExprLike(test: GetSubNode<TNode, ExprValue>(rootElement, reader, "Test"), pattern: GetSubNode<TNode, ExprStringLiteral>(rootElement, reader, "Pattern"));
                case "Merge": return new ExprMerge(targetTable: GetSubNode<TNode, ExprTable>(rootElement, reader, "TargetTable"), source: GetSubNode<TNode, IExprTableSource>(rootElement, reader, "Source"), on: GetSubNode<TNode, ExprBoolean>(rootElement, reader, "On"), whenMatched: GetNullableSubNode<TNode, IExprMergeMatched>(rootElement, reader, "WhenMatched"), whenNotMatchedByTarget: GetNullableSubNode<TNode, IExprMergeNotMatched>(rootElement, reader, "WhenNotMatchedByTarget"), whenNotMatchedBySource: GetNullableSubNode<TNode, IExprMergeMatched>(rootElement, reader, "WhenNotMatchedBySource"));
                case "MergeMatchedDelete": return new ExprMergeMatchedDelete(and: GetNullableSubNode<TNode, ExprBoolean>(rootElement, reader, "And"));
                case "MergeMatchedUpdate": return new ExprMergeMatchedUpdate(and: GetNullableSubNode<TNode, ExprBoolean>(rootElement, reader, "And"), set: GetSubNodeList<TNode, ExprColumnSetClause>(rootElement, reader, "Set"));
                case "MergeOutput": return new ExprMergeOutput(targetTable: GetSubNode<TNode, ExprTable>(rootElement, reader, "TargetTable"), source: GetSubNode<TNode, IExprTableSource>(rootElement, reader, "Source"), on: GetSubNode<TNode, ExprBoolean>(rootElement, reader, "On"), whenMatched: GetNullableSubNode<TNode, IExprMergeMatched>(rootElement, reader, "WhenMatched"), whenNotMatchedByTarget: GetNullableSubNode<TNode, IExprMergeNotMatched>(rootElement, reader, "WhenNotMatchedByTarget"), whenNotMatchedBySource: GetNullableSubNode<TNode, IExprMergeMatched>(rootElement, reader, "WhenNotMatchedBySource"), output: GetSubNode<TNode, ExprOutput>(rootElement, reader, "Output"));
                case "Modulo": return new ExprModulo(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "Mul": return new ExprMul(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "Null": return ExprNull.Instance;
                case "OffsetFetch": return new ExprOffsetFetch(offset: GetSubNode<TNode, ExprInt32Literal>(rootElement, reader, "Offset"), fetch: GetNullableSubNode<TNode, ExprInt32Literal>(rootElement, reader, "Fetch"));
                case "OrderBy": return new ExprOrderBy(orderList: GetSubNodeList<TNode, ExprOrderByItem>(rootElement, reader, "OrderList"));
                case "OrderByItem": return new ExprOrderByItem(value: GetSubNode<TNode, ExprValue>(rootElement, reader, "Value"), descendant: ReadBoolean(rootElement, reader, "Descendant"));
                case "OrderByOffsetFetch": return new ExprOrderByOffsetFetch(orderList: GetSubNodeList<TNode, ExprOrderByItem>(rootElement, reader, "OrderList"), offsetFetch: GetSubNode<TNode, ExprOffsetFetch>(rootElement, reader, "OffsetFetch"));
                case "Output": return new ExprOutput(columns: GetSubNodeList<TNode, IExprOutputColumn>(rootElement, reader, "Columns"));
                case "OutputAction": return new ExprOutputAction(alias: GetNullableSubNode<TNode, ExprColumnAlias>(rootElement, reader, "Alias"));
                case "OutputColumn": return new ExprOutputColumn(column: GetSubNode<TNode, ExprAliasedColumn>(rootElement, reader, "Column"));
                case "OutputColumnDeleted": return new ExprOutputColumnDeleted(columnName: GetSubNode<TNode, ExprAliasedColumnName>(rootElement, reader, "ColumnName"));
                case "OutputColumnInserted": return new ExprOutputColumnInserted(columnName: GetSubNode<TNode, ExprAliasedColumnName>(rootElement, reader, "ColumnName"));
                case "Over": return new ExprOver(partitions: GetNullableSubNodeList<TNode, ExprValue>(rootElement, reader, "Partitions"), orderBy: GetNullableSubNode<TNode, ExprOrderBy>(rootElement, reader, "OrderBy"), frameClause: GetNullableSubNode<TNode, ExprFrameClause>(rootElement, reader, "FrameClause"));
                case "QueryExpression": return new ExprQueryExpression(left: GetSubNode<TNode, IExprSubQuery>(rootElement, reader, "Left"), right: GetSubNode<TNode, IExprSubQuery>(rootElement, reader, "Right"), queryExpressionType: ReadExprQueryExpressionType(rootElement, reader, "QueryExpressionType"));
                case "QuerySpecification": return new ExprQuerySpecification(selectList: GetSubNodeList<TNode, IExprSelecting>(rootElement, reader, "SelectList"), top: GetNullableSubNode<TNode, ExprValue>(rootElement, reader, "Top"), from: GetNullableSubNode<TNode, IExprTableSource>(rootElement, reader, "From"), where: GetNullableSubNode<TNode, ExprBoolean>(rootElement, reader, "Where"), groupBy: GetNullableSubNodeList<TNode, ExprColumn>(rootElement, reader, "GroupBy"), distinct: ReadBoolean(rootElement, reader, "Distinct"));
                case "ScalarFunction": return new ExprScalarFunction(schema: GetNullableSubNode<TNode, ExprDbSchema>(rootElement, reader, "Schema"), name: GetSubNode<TNode, ExprFunctionName>(rootElement, reader, "Name"), arguments: GetNullableSubNodeList<TNode, ExprValue>(rootElement, reader, "Arguments"));
                case "SchemaName": return new ExprSchemaName(name: ReadString(rootElement, reader, "Name"));
                case "Select": return new ExprSelect(selectQuery: GetSubNode<TNode, IExprSubQuery>(rootElement, reader, "SelectQuery"), orderBy: GetSubNode<TNode, ExprOrderBy>(rootElement, reader, "OrderBy"));
                case "SelectOffsetFetch": return new ExprSelectOffsetFetch(selectQuery: GetSubNode<TNode, IExprSubQuery>(rootElement, reader, "SelectQuery"), orderBy: GetSubNode<TNode, ExprOrderByOffsetFetch>(rootElement, reader, "OrderBy"));
                case "StringConcat": return new ExprStringConcat(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "StringLiteral": return new ExprStringLiteral(value: ReadNullableString(rootElement, reader, "Value"));
                case "Sub": return new ExprSub(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "Sum": return new ExprSum(left: GetSubNode<TNode, ExprValue>(rootElement, reader, "Left"), right: GetSubNode<TNode, ExprValue>(rootElement, reader, "Right"));
                case "Table": return new ExprTable(fullName: GetSubNode<TNode, IExprTableFullName>(rootElement, reader, "FullName"), alias: GetNullableSubNode<TNode, ExprTableAlias>(rootElement, reader, "Alias"));
                case "TableAlias": return new ExprTableAlias(alias: GetSubNode<TNode, IExprAlias>(rootElement, reader, "Alias"));
                case "TableFullName": return new ExprTableFullName(dbSchema: GetNullableSubNode<TNode, ExprDbSchema>(rootElement, reader, "DbSchema"), tableName: GetSubNode<TNode, ExprTableName>(rootElement, reader, "TableName"));
                case "TableName": return new ExprTableName(name: ReadString(rootElement, reader, "Name"));
                case "TableValueConstructor": return new ExprTableValueConstructor(items: GetSubNodeList<TNode, ExprValueRow>(rootElement, reader, "Items"));
                case "TempTableName": return new ExprTempTableName(name: ReadString(rootElement, reader, "Name"));
                case "TypeBoolean": return ExprTypeBoolean.Instance;
                case "TypeByte": return ExprTypeByte.Instance;
                case "TypeDateTime": return new ExprTypeDateTime(isDate: ReadBoolean(rootElement, reader, "IsDate"));
                case "TypeDecimal": return new ExprTypeDecimal(precisionScale: ReadNullableDecimalPrecisionScale(rootElement, reader, "PrecisionScale"));
                case "TypeDouble": return ExprTypeDouble.Instance;
                case "TypeGuid": return ExprTypeGuid.Instance;
                case "TypeInt16": return ExprTypeInt16.Instance;
                case "TypeInt32": return ExprTypeInt32.Instance;
                case "TypeInt64": return ExprTypeInt64.Instance;
                case "TypeString": return new ExprTypeString(size: ReadNullableInt32(rootElement, reader, "Size"), isUnicode: ReadBoolean(rootElement, reader, "IsUnicode"), isText: ReadBoolean(rootElement, reader, "IsText"));
                case "UnboundedFrameBorder": return new ExprUnboundedFrameBorder(frameBorderDirection: ReadFrameBorderDirection(rootElement, reader, "FrameBorderDirection"));
                case "UnsafeValue": return new ExprUnsafeValue(unsafeValue: ReadString(rootElement, reader, "UnsafeValue"));
                case "Update": return new ExprUpdate(target: GetSubNode<TNode, ExprTable>(rootElement, reader, "Target"), setClause: GetSubNodeList<TNode, ExprColumnSetClause>(rootElement, reader, "SetClause"), source: GetNullableSubNode<TNode, IExprTableSource>(rootElement, reader, "Source"), filter: GetNullableSubNode<TNode, ExprBoolean>(rootElement, reader, "Filter"));
                case "ValueFrameBorder": return new ExprValueFrameBorder(value: GetSubNode<TNode, ExprValue>(rootElement, reader, "Value"), frameBorderDirection: ReadFrameBorderDirection(rootElement, reader, "FrameBorderDirection"));
                case "ValueRow": return new ExprValueRow(items: GetSubNodeList<TNode, ExprValue>(rootElement, reader, "Items"));
                //CodeGenEnd
                default: throw new SqExpressException($"Could not recognize the type tag \"{typeTag}\"");
            }
        }

        private static IReadOnlyList<TExpr> GetSubNodeList<TNode, TExpr>(TNode currentNode, IExprReader<TNode> reader, string name)
            where TExpr : class, IExpr
        {
            var enumerator = reader.EnumerateList(currentNode, name);

            if (enumerator == null)
            {
                throw new SqExpressException($"Property \"{name}\" is mandatory");
            }

            return enumerator.Select(i => DeserializeNodeStrongType<TNode, TExpr>(reader, i)).ToList();
        }

        private static IReadOnlyList<TExpr>? GetNullableSubNodeList<TNode, TExpr>(TNode currentNode, IExprReader<TNode> reader, string name)
            where TExpr : class, IExpr
        {
            return reader.EnumerateList(currentNode, name)?.Select(i => DeserializeNodeStrongType<TNode, TExpr>(reader, i)).ToList();
        }

        private static TExpr? GetNullableSubNode<TNode, TExpr>(TNode currentNode, IExprReader<TNode> reader, string name)
            where TExpr : class, IExpr
        {
            return reader.TryGetSubNode(currentNode, name, out var subNode) 
                ? DeserializeNodeStrongType<TNode, TExpr>(reader: reader, subNode: subNode) 
                : null;
        }

        private static TExpr GetSubNode<TNode, TExpr>(TNode currentNode, IExprReader<TNode> reader, string name)
            where TExpr : class, IExpr
        {
            return reader.TryGetSubNode(currentNode, name, out var subNode) 
                ? DeserializeNodeStrongType<TNode, TExpr>(reader: reader, subNode: subNode) 
                : throw new SqExpressException($"Property \"{name}\" is mandatory");
        }

        private static TExpr DeserializeNodeStrongType<TNode, TExpr>(IExprReader<TNode> reader, TNode subNode)
            where TExpr : class, IExpr
        {
            var subExpr = Deserialize(subNode, reader);
            if (subExpr is TExpr result)
            {
                return result;
            }

            throw new SqExpressException(
                $"Type of subexpression \"{subExpr.GetType().Name}\" does not match the expected type: \"{typeof(TExpr).Name}\"");
        }

        private static Guid ReadGuid<TNode>(TNode rootElement, IExprReader<TNode> reader, string name)
        {
            if (!reader.TryGetGuid(rootElement, name, out var result))
            {
                throw new SqExpressException($"Property \"{name}\" is mandatory");
            }
            return result;
        }

        private static string ReadString<TNode>(TNode rootElement, IExprReader<TNode> reader, string name)
        {
            if (!reader.TryGetString(rootElement, name, out var result))
            {
                throw new SqExpressException($"Property \"{name}\" is mandatory");
            }
            return result!;
        }

        private static bool ReadBoolean<TNode>(TNode rootElement, IExprReader<TNode> reader, string name)
        {
            if (!reader.TryGetBoolean(rootElement, name, out var result))
            {
                throw new SqExpressException($"Property \"{name}\" is mandatory");
            }
            return result;
        }

        private static int ReadInt32<TNode>(TNode rootElement, IExprReader<TNode> reader, string name)
        {
            if (!reader.TryGetInt32(rootElement, name, out var result))
            {
                throw new SqExpressException($"Property \"{name}\" is mandatory");
            }
            return result;
        }

        private static Guid? ReadNullableGuid<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetGuid(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static bool? ReadNullableBoolean<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetBoolean(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static string? ReadNullableString<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetString(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static short? ReadNullableInt16<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetInt16(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static int? ReadNullableInt32<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetInt32(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static long? ReadNullableInt64<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetInt64(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static double? ReadNullableDouble<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetDouble(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static decimal? ReadNullableDecimal<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetDecimal(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static DateTime? ReadNullableDateTime<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetDateTime(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static byte? ReadNullableByte<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetByte(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static IReadOnlyList<byte>? ReadNullableByteList<TNode>(TNode rootElement, IExprReader<TNode> reader, string value)
        {
            if (reader.TryGetByteArray(rootElement, value, out var result))
            {
                return result;
            }
            return null;
        }

        private static DecimalPrecisionScale? ReadNullableDecimalPrecisionScale<TNode>(TNode rootElement, IExprReader<TNode> reader, string name)
        {
            var precision = ReadNullableInt32(rootElement, reader, name + "." + nameof(DecimalPrecisionScale.Precision));
            if (!precision.HasValue)
            {
                return null;
            }

            var scale = ReadNullableInt32(rootElement, reader, name + "." + nameof(DecimalPrecisionScale.Scale));
            return new DecimalPrecisionScale(precision.Value, scale);
        }

        private static ExprQueryExpressionType ReadExprQueryExpressionType<TNode>(TNode rootElement, IExprReader<TNode> reader, string name)
        {
            var str = ReadNullableString(rootElement, reader, name);
            if (str == null)
            {
                throw new SqExpressException($"Property \"{name}\" is mandatory");
            }

            if (!Enum.TryParse(str, false, out ExprQueryExpressionType result))
            {
                throw new SqExpressException($"Could not recognize \"{str}\" as \"{nameof(ExprQueryExpressionType)}\"");
            }

            return result;
        }

        private static ExprJoinedTable.ExprJoinType ReadExprJoinType<TNode>(TNode rootElement, IExprReader<TNode> reader, string name)
        {
            var str = ReadNullableString(rootElement, reader, name);
            if (str == null)
            {
                throw new SqExpressException($"Property \"{name}\" is mandatory");
            }

            if (!Enum.TryParse(str, false, out ExprJoinedTable.ExprJoinType result))
            {
                throw new SqExpressException($"Could not recognize \"{str}\" as \"{nameof(ExprJoinedTable.ExprJoinType)}\"");
            }

            return result;
        }

        private static DateAddDatePart ReadDateAddDatePart<TNode>(TNode rootElement, IExprReader<TNode> reader, string name)
        {
            var str = ReadNullableString(rootElement, reader, name);
            if (str == null)
            {
                throw new SqExpressException($"Property \"{name}\" is mandatory");
            }

            if (!Enum.TryParse(str, false, out DateAddDatePart result))
            {
                throw new SqExpressException($"Could not recognize \"{str}\" as \"{nameof(DateAddDatePart)}\"");
            }

            return result;
        }

        private static FrameBorderDirection ReadFrameBorderDirection<TNode>(TNode rootElement, IExprReader<TNode> reader, string name)
        {
            var str = ReadNullableString(rootElement, reader, name);
            if (str == null)
            {
                throw new SqExpressException($"Property \"{name}\" is mandatory");
            }

            if (!Enum.TryParse(str, false, out FrameBorderDirection result))
            {
                throw new SqExpressException($"Could not recognize \"{str}\" as \"{nameof(FrameBorderDirection)}\"");
            }

            return result;
        }
    }
}
