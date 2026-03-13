using System;
using SqExpress.Syntax.Expressions;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
using SqExpress.Syntax.Names;
using SqExpress.Syntax.Select;
using SqExpress.Syntax.Select.SelectItems;
using SqExpress.Syntax.Type;
using SqExpress.Syntax.Value;

namespace SqExpress.SyntaxTreeOperations.Internal;

internal sealed class ExprSelectingToColumnInfo : IExprSelectingVisitor<ExprSelectingAsColumnInfo?, object?>
{
    public static readonly ExprSelectingToColumnInfo Instance = new ExprSelectingToColumnInfo();

    private ExprSelectingToColumnInfo()
    {
    }

    public ExprSelectingAsColumnInfo? VisitExprInt32Literal(ExprInt32Literal exprInt32Literal, object? arg)
        => FromValue(exprInt32Literal);

    public ExprSelectingAsColumnInfo? VisitExprGuidLiteral(ExprGuidLiteral exprGuidLiteral, object? arg)
        => FromValue(exprGuidLiteral);

    public ExprSelectingAsColumnInfo? VisitExprStringLiteral(ExprStringLiteral stringLiteral, object? arg)
        => FromValue(stringLiteral);

    public ExprSelectingAsColumnInfo? VisitExprDateTimeLiteral(ExprDateTimeLiteral dateTimeLiteral, object? arg)
        => FromValue(dateTimeLiteral);

    public ExprSelectingAsColumnInfo? VisitExprDateTimeOffsetLiteral(ExprDateTimeOffsetLiteral dateTimeLiteral, object? arg)
        => FromValue(dateTimeLiteral);

    public ExprSelectingAsColumnInfo? VisitExprBoolLiteral(ExprBoolLiteral boolLiteral, object? arg)
        => FromValue(boolLiteral);

    public ExprSelectingAsColumnInfo? VisitExprInt64Literal(ExprInt64Literal int64Literal, object? arg)
        => FromValue(int64Literal);

    public ExprSelectingAsColumnInfo? VisitExprByteLiteral(ExprByteLiteral byteLiteral, object? arg)
        => FromValue(byteLiteral);

    public ExprSelectingAsColumnInfo? VisitExprInt16Literal(ExprInt16Literal int16Literal, object? arg)
        => FromValue(int16Literal);

    public ExprSelectingAsColumnInfo? VisitExprDecimalLiteral(ExprDecimalLiteral decimalLiteral, object? arg)
        => FromValue(decimalLiteral);

    public ExprSelectingAsColumnInfo? VisitExprDoubleLiteral(ExprDoubleLiteral doubleLiteral, object? arg)
        => FromValue(doubleLiteral);

    public ExprSelectingAsColumnInfo? VisitExprByteArrayLiteral(ExprByteArrayLiteral byteArrayLiteral, object? arg)
        => FromValue(byteArrayLiteral);

    public ExprSelectingAsColumnInfo? VisitExprNull(ExprNull exprNull, object? arg)
        => null;

    public ExprSelectingAsColumnInfo? VisitExprUnsafeValue(ExprUnsafeValue exprUnsafeValue, object? arg)
        => null;

    public ExprSelectingAsColumnInfo? VisitExprValueQuery(ExprValueQuery exprValueQuery, object? arg)
    {
        var outputSelecting = exprValueQuery.Query.ExtractSelecting();
        return outputSelecting.Count == 1 ? outputSelecting[0].Accept(this, arg) : null;
    }

    public ExprSelectingAsColumnInfo? VisitExprSum(ExprSum exprSum, object? arg)
        => FromValue(exprSum);

    public ExprSelectingAsColumnInfo? VisitExprSub(ExprSub exprSub, object? arg)
        => FromValue(exprSub);

    public ExprSelectingAsColumnInfo? VisitExprMul(ExprMul exprMul, object? arg)
        => FromValue(exprMul);

    public ExprSelectingAsColumnInfo? VisitExprDiv(ExprDiv exprDiv, object? arg)
        => FromValue(exprDiv);

    public ExprSelectingAsColumnInfo? VisitExprModulo(ExprModulo exprModulo, object? arg)
        => FromValue(exprModulo);

    public ExprSelectingAsColumnInfo? VisitExprStringConcat(ExprStringConcat exprStringConcat, object? arg)
        => FromValue(exprStringConcat);

    public ExprSelectingAsColumnInfo? VisitExprBitwiseNot(ExprBitwiseNot exprBitwiseNot, object? arg)
        => FromValue(exprBitwiseNot);

    public ExprSelectingAsColumnInfo? VisitExprBitwiseAnd(ExprBitwiseAnd exprBitwiseAnd, object? arg)
        => FromValue(exprBitwiseAnd);

    public ExprSelectingAsColumnInfo? VisitExprBitwiseXor(ExprBitwiseXor exprBitwiseXor, object? arg)
        => FromValue(exprBitwiseXor);

    public ExprSelectingAsColumnInfo? VisitExprBitwiseOr(ExprBitwiseOr exprBitwiseOr, object? arg)
        => FromValue(exprBitwiseOr);

    public ExprSelectingAsColumnInfo? VisitExprScalarFunction(ExprScalarFunction exprScalarFunction, object? arg)
        => FromValue(exprScalarFunction);

    public ExprSelectingAsColumnInfo? VisitExprPortableScalarFunction(ExprPortableScalarFunction exprPortableScalarFunction, object? arg)
        => FromValue(exprPortableScalarFunction);

    public ExprSelectingAsColumnInfo? VisitExprCase(ExprCase exprCase, object? arg)
    {
        var result = exprCase.DefaultValue.Accept(this, arg);
        if (result != null)
        {
            return result;
        }

        for (var i = 0; i < exprCase.Cases.Count; i++)
        {
            result = exprCase.Cases[i].Accept(this, arg);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public ExprSelectingAsColumnInfo? VisitExprCaseWhenThen(ExprCaseWhenThen exprCaseWhenThen, object? arg)
        => exprCaseWhenThen.Value.Accept(this, arg);

    public ExprSelectingAsColumnInfo? VisitExprFuncIsNull(ExprFuncIsNull exprFuncIsNull, object? arg)
        => FirstNonNull(exprFuncIsNull.Test.Accept(this, arg), exprFuncIsNull.Alt.Accept(this, arg));

    public ExprSelectingAsColumnInfo? VisitExprFuncCoalesce(ExprFuncCoalesce exprFuncCoalesce, object? arg)
    {
        var result = exprFuncCoalesce.Test.Accept(this, arg);
        if (result != null)
        {
            return result;
        }

        for (var i = 0; i < exprFuncCoalesce.Alts.Count; i++)
        {
            result = exprFuncCoalesce.Alts[i].Accept(this, arg);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }

    public ExprSelectingAsColumnInfo? VisitExprGetDate(ExprGetDate exprGetDate, object? arg)
        => (SqQueryBuilder.SqlType.DateTime(false), false);

    public ExprSelectingAsColumnInfo? VisitExprGetUtcDate(ExprGetUtcDate exprGetUtcDate, object? arg)
        => (SqQueryBuilder.SqlType.DateTime(false), false);

    public ExprSelectingAsColumnInfo? VisitExprDateAdd(ExprDateAdd exprDateAdd, object? arg)
        => (SqQueryBuilder.SqlType.DateTime(false), false);

    public ExprSelectingAsColumnInfo? VisitExprDateDiff(ExprDateDiff exprDateDiff, object? arg)
        => (SqQueryBuilder.SqlType.Int32, false);

    public ExprSelectingAsColumnInfo? VisitExprColumn(ExprColumn exprColumn, object? arg)
        => exprColumn;

    public ExprSelectingAsColumnInfo? VisitExprCast(ExprCast exprCast, object? arg)
        => (exprCast.SqlType, null);

    public ExprSelectingAsColumnInfo? VisitExprParameter(ExprParameter exprParameter, object? arg)
        => exprParameter.ReplacedValue?.Accept(this, arg);

    public ExprSelectingAsColumnInfo? VisitExprAllColumns(ExprAllColumns exprAllColumns, object? arg)
        => null;

    public ExprSelectingAsColumnInfo? VisitExprColumnName(ExprColumnName columnName, object? arg)
        => columnName.Name;

    public ExprSelectingAsColumnInfo? VisitExprAliasedColumn(ExprAliasedColumn exprAliasedColumn, object? arg)
        => exprAliasedColumn.Alias != null
            ? exprAliasedColumn.Column.WithColumnName(exprAliasedColumn.Alias.Name)
            : exprAliasedColumn.Column;

    public ExprSelectingAsColumnInfo? VisitExprAliasedColumnName(ExprAliasedColumnName exprAliasedColumnName, object? arg)
        => exprAliasedColumnName.Alias?.Name ?? exprAliasedColumnName.Column.Name;

    public ExprSelectingAsColumnInfo? VisitExprAliasedSelecting(ExprAliasedSelecting exprAliasedSelecting, object? arg)
    {
        var exprSelectingAsColumnInfo = exprAliasedSelecting.Value.Accept(this, arg);

        if (exprSelectingAsColumnInfo == null)
        {
            return null;
        }
        if (exprSelectingAsColumnInfo.IsColumn)
        {
            return exprSelectingAsColumnInfo.AsColumn().WithColumnName(exprAliasedSelecting.Alias.Name);
        }
        if (exprSelectingAsColumnInfo.IsType)
        {
            var (type, isNullable) = exprSelectingAsColumnInfo.AsType();
            return type.Accept(ExprTypeToCustomColumn.Instance, (exprAliasedSelecting.Alias.Name, isNullable));
        }

        return exprSelectingAsColumnInfo;
    }

    public ExprSelectingAsColumnInfo? VisitExprAggregateFunction(ExprAggregateFunction exprAggregateFunction, object? arg)
    {
        switch (exprAggregateFunction.Name.Name.ToUpperInvariant())
        {
            case "COUNT":
                return (SqQueryBuilder.SqlType.Int32 , false);

            case "MIN":
            case "MAX":
            case "SUM":
            case "AVG":
                return exprAggregateFunction.Expression.Accept(this, arg);

            default:
                return null;
        }
    }

    public ExprSelectingAsColumnInfo? VisitExprAggregateOverFunction(ExprAggregateOverFunction exprAggregateFunction, object? arg)
        => exprAggregateFunction.Function.Accept(this, arg);

    public ExprSelectingAsColumnInfo? VisitExprAnalyticFunction(ExprAnalyticFunction exprAnalyticFunction, object? arg)
    {
        switch (exprAnalyticFunction.Name.Name.ToUpperInvariant())
        {
            case "ROW_NUMBER":
            case "RANK":
            case "DENSE_RANK":
            case "NTILE":
                return (SqQueryBuilder.SqlType.Int32, false);

            case "CUME_DIST":
            case "PERCENT_RANK":
                return (SqQueryBuilder.SqlType.Double, false);

            case "FIRST_VALUE":
            case "LAST_VALUE":
            case "LAG":
            case "LEAD":
                return exprAnalyticFunction.Arguments != null && exprAnalyticFunction.Arguments.Count > 0
                    ? exprAnalyticFunction.Arguments[0].Accept(this, arg)
                    : null;

            default:
                return null;
        }
    }

    private static ExprSelectingAsColumnInfo? FromValue(ExprValue exprValue)
    {
        if (exprValue is ExprColumn exprColumn)
        {
            return exprColumn;
        }

        var typeDetails = exprValue.GetTypeDetails();
        return typeDetails.Type != null ? new ExprSelectingAsColumnInfo(typeDetails.Type, typeDetails.IsNull) : null;
    }

    private static ExprSelectingAsColumnInfo? FirstNonNull(ExprSelectingAsColumnInfo? first, ExprSelectingAsColumnInfo? second)
        => first ?? second;
}

internal sealed class ExprSelectingAsColumnInfo
{
    private readonly object _value;

    private readonly bool? _isNullable;

    internal ExprSelectingAsColumnInfo(object value, bool? isNullable = false)
    {
        this._value = value ?? throw new ArgumentNullException(nameof(value));
        this._isNullable = isNullable;
    }

    public bool IsColumn => this._value is ExprColumn;
    public bool IsString => this._value is string;
    public bool IsType => this._value is ExprType;

    public ExprColumn AsColumn() => (ExprColumn)this._value;
    public string AsString() => (string)this._value;
    public (ExprType Type, bool? IsNullable) AsType() => ((ExprType)this._value, this._isNullable);

    public T Match<T>(
        Func<ExprColumn, T> column,
        Func<string, T> str,
        Func<ExprType, T> type)
    {
        return this._value switch
        {
            ExprColumn exprColumn => column(exprColumn),
            string s => str(s),
            ExprType exprType => type(exprType),
            _ => throw new InvalidOperationException("Unsupported union value")
        };
    }

    public static implicit operator ExprSelectingAsColumnInfo(ExprColumn value) => new ExprSelectingAsColumnInfo(value);
    public static implicit operator ExprSelectingAsColumnInfo(string value) => new ExprSelectingAsColumnInfo(value);
    public static implicit operator ExprSelectingAsColumnInfo((ExprType, bool?) value) => new ExprSelectingAsColumnInfo(value.Item1, value.Item2);
}
