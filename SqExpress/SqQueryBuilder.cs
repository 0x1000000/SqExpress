﻿using System;
using System.Collections.Generic;
using SqExpress.QueryBuilders;
using SqExpress.QueryBuilders.Case;
using SqExpress.QueryBuilders.Delete;
using SqExpress.QueryBuilders.Insert;
using SqExpress.QueryBuilders.Insert.Internal;
using SqExpress.QueryBuilders.Merge;
using SqExpress.QueryBuilders.Merge.Internal;
using SqExpress.QueryBuilders.Update;
using SqExpress.QueryBuilders.Update.Internal;
using SqExpress.Syntax.Boolean.Predicate;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Update;
using SqExpress.Syntax.Value;
using SqExpress.Utils;

namespace SqExpress
{
    public static partial class SqQueryBuilder
    {
        public static ExprNull Null => ExprNull.Instance;

        public static ExprDefault Default => ExprDefault.Instance;

        public static ExprUnsafeValue UnsafeValue(string unsafeValueExpr) => new ExprUnsafeValue(unsafeValueExpr);

        public static ExprIsNull IsNull(ExprValue value) => new ExprIsNull(value, not: false);

        public static ExprIsNull IsNotNull(ExprValue value) => new ExprIsNull(value, not: true);

        public static ExprLike Like(ExprValue test, string pattern) => new ExprLike(test, pattern);

        public static SqlTypeSelector SqlType => new SqlTypeSelector();

        public static CaseWhen Case() => new CaseWhen();

        public struct SqlTypeSelector
        {
            public ExprTypeBoolean Boolean => ExprTypeBoolean.Instance;
            public ExprTypeByte Byte => ExprTypeByte.Instance;
            public ExprTypeInt16 Int16 => ExprTypeInt16.Instance;
            public ExprTypeInt32 Int32 => ExprTypeInt32.Instance;
            public ExprTypeInt64 Int64 => ExprTypeInt64.Instance;
            public ExprTypeDecimal Decimal(DecimalPrecisionScale? precisionScale = null) => new ExprTypeDecimal(precisionScale);
            public ExprTypeDouble Double => ExprTypeDouble.Instance;
            public ExprTypeDateTime DateTime(bool isDate = false) => new ExprTypeDateTime(isDate);
            public ExprTypeDateTimeOffset DateTimeOffset => ExprTypeDateTimeOffset.Instance;
            public ExprTypeGuid Guid => ExprTypeGuid.Instance;
            public ExprTypeString String(int? size=null, bool isUnicode=true, bool isText = false) =>new ExprTypeString(size, isUnicode, isText);
            public ExprTypeFixSizeByteArray ByteArrayFixedSize(int size) => new ExprTypeFixSizeByteArray(size);
            public ExprTypeByteArray ByteArray(int? size) => new ExprTypeByteArray(size);

        }

        public static ExprCast Cast(ExprValue expression, ExprType asType) 
            => new ExprCast(expression, asType);

        public static ExprCast Cast(IExprSelecting expression, ExprType asType) 
            => new ExprCast(expression, asType);

        public static ExprColumn Column(string columnName) 
            => new ExprColumn(null, columnName);

        public static ExprColumn Column(IExprColumnSource source, string columnName) 
            => new ExprColumn(source, columnName);

        public static ExprTableAlias TableAlias(Alias alias = default)
            => new ExprTableAlias(alias.BuildAliasExpression() ?? Alias.Auto.BuildAliasExpression()!);

        public static ExprExists Exists(IExprSubQueryFinal subQuery) 
            => new ExprExists(subQuery.Done());

        public static IInsertDataBuilderMapData<TTable, TItem> InsertDataInto<TTable, TItem>(TTable table, IEnumerable<TItem> data)
            where TTable : ExprTable 
            =>
            new InsertDataBuilder<TTable, TItem>(table, data);

        public static InsertBuilder InsertInto(ExprTable table, ExprColumnName column1, params ExprColumnName[] rest)
            => new InsertBuilder(table, Helpers.Combine(column1, rest));

        public static InsertBuilder InsertInto(ExprTable table, IReadOnlyList<ExprColumnName> columns)
            => new InsertBuilder(table, columns.AssertNotEmpty(nameof(columns)));

        public static InsertBuilder InsertInto(ExprTable table, IReadOnlyList<ExprColumn> columns)
            => new InsertBuilder(table, columns.AssertNotEmpty(nameof(columns)).SelectToReadOnlyList(x=>x.ColumnName));

        public static IdentityInsertBuilder IdentityInsertInto(ExprTable table, ExprColumnName column1, params ExprColumnName[] rest)
            => new IdentityInsertBuilder(table, Helpers.Combine(column1, rest));

        public static IdentityInsertBuilder IdentityInsertInto(ExprTable table, IReadOnlyList<ExprColumnName> columns)
            => new IdentityInsertBuilder(table, columns.AssertNotEmpty(nameof(columns)));

        public static IdentityInsertBuilder IdentityInsertInto(ExprTable table, IReadOnlyList<ExprColumn> columns)
            => new IdentityInsertBuilder(table, columns.AssertNotEmpty(nameof(columns)).SelectToReadOnlyList(x => x.ColumnName));

        public static UpdateBuilder Update(ExprTable target)
            => new UpdateBuilder(target, new List<ExprColumnSetClause>());

        public static IUpdateDataBuilderMapDataInitial<TTable, TItem> UpdateData<TTable, TItem>(TTable table, IEnumerable<TItem> data)
            where TTable : ExprTable
            => new UpdateDataBuilder<TTable,TItem>(table, data, new ExprAliasGuid(Guid.NewGuid()));

        public static IMergeDataBuilderMapDataInitial<TTable, TItem> MergeDataInto<TTable, TItem>(TTable table, IEnumerable<TItem> data)
            where TTable : ExprTable
            => new MergeDataBuilder<TTable, TItem>(table, data, new ExprAliasGuid(Guid.NewGuid()));

        public static IMergeBuilderCondition MergeInto(ExprTable target, IExprTableSource source)
            => new MergeBuilder(target, source);

        public static DeleteBuilder Delete(ExprTable target) 
            => new DeleteBuilder(target: target);
    }
}