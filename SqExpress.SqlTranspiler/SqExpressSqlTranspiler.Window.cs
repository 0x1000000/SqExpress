using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SqExpress.SqlTranspiler
{
    public sealed partial class SqExpressSqlTranspiler
    {
        private ExpressionSyntax BuildWindowFunctionCall(FunctionCall functionCall, TranspileContext context, bool wrapLiterals)
        {
            if (functionCall.OverClause == null)
            {
                throw new SqExpressSqlTranspilerException("Expected OVER clause for window function.");
            }

            if (functionCall.OverClause.WindowName != null)
            {
                throw new SqExpressSqlTranspilerException("Named windows in OVER clause are not supported yet.");
            }

            var functionName = functionCall.FunctionName.Value;
            var normalizedName = functionName.ToUpperInvariant();

            if (IsKnownAggregateFunctionName(normalizedName) || functionCall.UniqueRowFilter == UniqueRowFilter.Distinct)
            {
                if (functionCall.OverClause.WindowFrameClause != null)
                {
                    return this.BuildAggregateWindowFunctionWithFrameHelpers(functionCall, context, wrapLiterals);
                }

                var aggregateFunction = this.BuildAggregateFunctionCall(functionCall, context, wrapLiterals);
                return this.ApplyWindowOverToAggregate(aggregateFunction, functionCall.OverClause, context);
            }

            return this.BuildAnalyticWindowFunction(functionCall, context, wrapLiterals);
        }

        private ExpressionSyntax BuildAggregateFunctionCall(FunctionCall functionCall, TranspileContext context, bool wrapLiterals)
        {
            var functionName = functionCall.FunctionName.Value;
            var normalizedName = functionName.ToUpperInvariant();
            var distinct = functionCall.UniqueRowFilter == UniqueRowFilter.Distinct;

            switch (normalizedName)
            {
                case "COUNT":
                {
                    if (functionCall.Parameters.Count == 1 && IsStar(functionCall.Parameters[0]))
                    {
                        if (distinct)
                        {
                            throw new SqExpressSqlTranspilerException("COUNT(DISTINCT *) is not valid.");
                        }

                        return Invoke("CountOne");
                    }

                    if (functionCall.Parameters.Count != 1)
                    {
                        throw new SqExpressSqlTranspilerException("COUNT supports exactly one argument.");
                    }

                    var countArg = this.BuildScalarExpression(functionCall.Parameters[0], context, wrapLiterals);
                    return distinct
                        ? Invoke("CountDistinct", countArg)
                        : Invoke("Count", countArg);
                }
                case "MIN":
                case "MAX":
                case "SUM":
                case "AVG":
                {
                    if (functionCall.Parameters.Count != 1)
                    {
                        throw new SqExpressSqlTranspilerException($"Aggregate function '{functionName}' supports exactly one argument.");
                    }

                    var arg = functionCall.Parameters[0];
                    if (IsStar(arg))
                    {
                        throw new SqExpressSqlTranspilerException($"Aggregate function '{functionName}' does not support '*'.");
                    }

                    var value = this.BuildScalarExpression(arg, context, wrapLiterals);
                    return normalizedName switch
                    {
                        "MIN" => Invoke(distinct ? "MinDistinct" : "Min", value),
                        "MAX" => Invoke(distinct ? "MaxDistinct" : "Max", value),
                        "SUM" => Invoke(distinct ? "SumDistinct" : "Sum", value),
                        "AVG" => Invoke(distinct ? "AvgDistinct" : "Avg", value),
                        _ => throw new SqExpressSqlTranspilerException($"Unsupported aggregate function '{functionName}'.")
                    };
                }
                default:
                {
                    if (functionCall.Parameters.Count != 1)
                    {
                        throw new SqExpressSqlTranspilerException($"Aggregate function '{functionName}' with OVER supports exactly one argument.");
                    }

                    var arg = functionCall.Parameters[0];
                    if (IsStar(arg))
                    {
                        throw new SqExpressSqlTranspilerException($"Aggregate function '{functionName}' with '*' argument is not supported.");
                    }

                    var value = this.BuildScalarExpression(arg, context, wrapLiterals);
                    return Invoke(
                        "AggregateFunction",
                        StringLiteral(functionName),
                        distinct
                            ? LiteralExpression(SyntaxKind.TrueLiteralExpression)
                            : LiteralExpression(SyntaxKind.FalseLiteralExpression),
                        value);
                }
            }
        }

        private ExpressionSyntax ApplyWindowOverToAggregate(ExpressionSyntax aggregateFunction, OverClause overClause, TranspileContext context)
        {
            var partitions = this.BuildWindowPartitionExpressions(overClause, context);
            var orderItems = this.BuildWindowOrderByItemExpressions(overClause.OrderByClause, context);
            return ApplyWindowOver(
                aggregateFunction,
                partitions,
                orderItems,
                noPartitionNoOrderMethod: "Over",
                partitionNoOrderMethod: "NoOrderBy",
                noPartitionOrderMethod: "OverOrderBy",
                partitionOrderMethod: "OrderBy");
        }

        private ExpressionSyntax BuildAggregateWindowFunctionWithFrameHelpers(FunctionCall functionCall, TranspileContext context, bool wrapLiterals)
        {
            var overClause = functionCall.OverClause
                ?? throw new SqExpressSqlTranspilerException("Expected OVER clause for aggregate window function.");

            var frameClause = overClause.WindowFrameClause
                ?? throw new SqExpressSqlTranspilerException("Expected frame clause for aggregate window function.");

            EnsureRowsFrame(frameClause);

            var aggregateFunction = this.BuildAggregateFunctionCall(functionCall, context, wrapLiterals);
            var partitions = this.BuildWindowPartitionExpressions(overClause, context);
            var orderItems = this.BuildWindowOrderByItemExpressions(overClause.OrderByClause, context);
            var baseWindow = ApplyWindowOver(
                aggregateFunction,
                partitions,
                orderItems,
                noPartitionNoOrderMethod: "Over",
                partitionNoOrderMethod: "NoOrderBy",
                noPartitionOrderMethod: "OverOrderBy",
                partitionOrderMethod: "OrderBy");

            var start = this.BuildFrameBorderHelper(frameClause.Top, context);
            var end = frameClause.Bottom == null
                ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : this.BuildFrameBorderHelper(frameClause.Bottom, context);

            return InvokeMember(baseWindow, "FrameClause", start, end);
        }

        private ExpressionSyntax BuildAnalyticWindowFunction(FunctionCall functionCall, TranspileContext context, bool wrapLiterals)
        {
            var overClause = functionCall.OverClause
                ?? throw new SqExpressSqlTranspilerException("Expected OVER clause for analytic function.");

            if (!this.TryBuildAnalyticWindowFunctionBuilder(functionCall, context, wrapLiterals, out var builderExpression, out var frameBuilder))
            {
                return this.BuildAnalyticWindowFunctionLowLevel(functionCall, context, wrapLiterals);
            }

            var partitions = this.BuildWindowPartitionExpressions(overClause, context);
            var orderItems = this.BuildWindowOrderByItemExpressions(overClause.OrderByClause, context);

            if (frameBuilder)
            {
                if (orderItems.Count == 0)
                {
                    return this.BuildAnalyticWindowFunctionLowLevel(functionCall, context, wrapLiterals);
                }

                var overBuilder = BuildAnalyticOverBuilder(builderExpression, partitions, orderItems);

                if (overClause.WindowFrameClause == null)
                {
                    return InvokeMember(overBuilder, "FrameClauseEmpty");
                }

                EnsureRowsFrame(overClause.WindowFrameClause);

                var start = this.BuildFrameBorderHelper(overClause.WindowFrameClause.Top, context);
                var end = overClause.WindowFrameClause.Bottom == null
                    ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                    : this.BuildFrameBorderHelper(overClause.WindowFrameClause.Bottom, context);

                return InvokeMember(overBuilder, "FrameClause", start, end);
            }

            if (overClause.WindowFrameClause != null)
            {
                return this.BuildAnalyticWindowFunctionLowLevel(functionCall, context, wrapLiterals);
            }

            if (orderItems.Count == 0)
            {
                return this.BuildAnalyticWindowFunctionLowLevel(functionCall, context, wrapLiterals);
            }

            return BuildAnalyticOverBuilder(builderExpression, partitions, orderItems);
        }

        private static ExpressionSyntax ApplyWindowOver(
            ExpressionSyntax baseExpression,
            IReadOnlyList<ExpressionSyntax> partitions,
            IReadOnlyList<ExpressionSyntax> orderItems,
            string noPartitionNoOrderMethod,
            string partitionNoOrderMethod,
            string noPartitionOrderMethod,
            string partitionOrderMethod)
        {
            if (partitions.Count == 0)
            {
                return orderItems.Count == 0
                    ? InvokeMember(baseExpression, noPartitionNoOrderMethod)
                    : InvokeMember(baseExpression, noPartitionOrderMethod, orderItems);
            }

            var partitioned = InvokeMember(baseExpression, "OverPartitionBy", partitions);
            return orderItems.Count == 0
                ? InvokeMember(partitioned, partitionNoOrderMethod)
                : InvokeMember(partitioned, partitionOrderMethod, orderItems);
        }

        private static ExpressionSyntax BuildAnalyticOverBuilder(
            ExpressionSyntax builderExpression,
            IReadOnlyList<ExpressionSyntax> partitions,
            IReadOnlyList<ExpressionSyntax> orderItems)
        {
            if (partitions.Count == 0)
            {
                return InvokeMember(builderExpression, "OverOrderBy", orderItems);
            }

            return InvokeMember(
                InvokeMember(builderExpression, "OverPartitionBy", partitions),
                "OverOrderBy",
                orderItems);
        }

        private static void EnsureRowsFrame(WindowFrameClause frameClause)
        {
            if (frameClause.WindowFrameType == WindowFrameType.Range)
            {
                throw new SqExpressSqlTranspilerException("RANGE window frame is not supported yet. Use ROWS frame.");
            }
        }

        private bool TryBuildAnalyticWindowFunctionBuilder(
            FunctionCall functionCall,
            TranspileContext context,
            bool wrapLiterals,
            out ExpressionSyntax builderExpression,
            out bool frameBuilder)
        {
            var functionName = functionCall.FunctionName.Value;
            var normalizedName = functionName.ToUpperInvariant();
            var arguments = this.BuildFunctionArguments(functionCall.Parameters, context, wrapLiterals);

            frameBuilder = false;
            builderExpression = default!;

            switch (normalizedName)
            {
                case "ROW_NUMBER":
                {
                    if (arguments.Count != 0)
                    {
                        return false;
                    }

                    builderExpression = Invoke("RowNumber");
                    return true;
                }
                case "RANK":
                {
                    if (arguments.Count != 0)
                    {
                        return false;
                    }

                    builderExpression = Invoke("Rank");
                    return true;
                }
                case "DENSE_RANK":
                {
                    if (arguments.Count != 0)
                    {
                        return false;
                    }

                    builderExpression = Invoke("DenseRank");
                    return true;
                }
                case "CUME_DIST":
                {
                    if (arguments.Count != 0)
                    {
                        return false;
                    }

                    builderExpression = Invoke("CumeDist");
                    return true;
                }
                case "PERCENT_RANK":
                {
                    if (arguments.Count != 0)
                    {
                        return false;
                    }

                    builderExpression = Invoke("PercentRank");
                    return true;
                }
                case "NTILE":
                {
                    if (arguments.Count != 1)
                    {
                        return false;
                    }

                    builderExpression = Invoke("Ntile", arguments[0]);
                    return true;
                }
                case "LAG":
                {
                    if (arguments.Count == 0 || arguments.Count > 3)
                    {
                        return false;
                    }

                    builderExpression = arguments.Count switch
                    {
                        1 => Invoke("Lag", arguments[0]),
                        2 => Invoke("Lag", arguments[0], arguments[1]),
                        3 => Invoke("Lag", arguments[0], arguments[1], arguments[2]),
                        _ => throw new SqExpressSqlTranspilerException("Unexpected LAG argument count.")
                    };
                    return true;
                }
                case "LEAD":
                {
                    if (arguments.Count == 0 || arguments.Count > 3)
                    {
                        return false;
                    }

                    builderExpression = arguments.Count switch
                    {
                        1 => Invoke("Lead", arguments[0]),
                        2 => Invoke("Lead", arguments[0], arguments[1]),
                        3 => Invoke("Lead", arguments[0], arguments[1], arguments[2]),
                        _ => throw new SqExpressSqlTranspilerException("Unexpected LEAD argument count.")
                    };
                    return true;
                }
                case "FIRST_VALUE":
                {
                    if (arguments.Count != 1)
                    {
                        return false;
                    }

                    frameBuilder = true;
                    builderExpression = Invoke("FirstValue", arguments[0]);
                    return true;
                }
                case "LAST_VALUE":
                {
                    if (arguments.Count != 1)
                    {
                        return false;
                    }

                    frameBuilder = true;
                    builderExpression = Invoke("LastValue", arguments[0]);
                    return true;
                }
                default:
                {
                    if (functionCall.OverClause?.WindowFrameClause != null)
                    {
                        if (arguments.Count == 0)
                        {
                            return false;
                        }

                        frameBuilder = true;
                        builderExpression = Invoke("AnalyticFunctionFrame", Prepend(StringLiteral(functionName), arguments));
                        return true;
                    }

                    builderExpression = arguments.Count == 0
                        ? Invoke("AnalyticFunction", StringLiteral(functionName))
                        : Invoke("AnalyticFunction", Prepend(StringLiteral(functionName), arguments));
                    return true;
                }
            }
        }

        private ExpressionSyntax BuildAnalyticWindowFunctionLowLevel(FunctionCall functionCall, TranspileContext context, bool wrapLiterals)
        {
            var functionName = functionCall.FunctionName.Value;
            var analyticArgs = this.BuildFunctionArguments(functionCall.Parameters, context, wrapLiterals);
            var argsExpr = analyticArgs.Count == 0
                ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : this.BuildExprValueArray(analyticArgs);
            var overExpression = this.BuildOverClause(functionCall.OverClause!, context);

            return Invoke("AnalyticFunction", StringLiteral(functionName), argsExpr, overExpression);
        }

        private IReadOnlyList<ExpressionSyntax> BuildWindowPartitionExpressions(OverClause overClause, TranspileContext context)
        {
            return overClause.Partitions
                .Select(partition => this.BuildScalarExpression(partition, context, wrapLiterals: false))
                .ToList();
        }

        private IReadOnlyList<ExpressionSyntax> BuildWindowOrderByItemExpressions(OrderByClause? orderByClause, TranspileContext context)
        {
            if (orderByClause == null)
            {
                return Array.Empty<ExpressionSyntax>();
            }

            if (orderByClause.OrderByElements.Count == 0)
            {
                throw new SqExpressSqlTranspilerException("OVER ORDER BY list cannot be empty.");
            }

            return orderByClause.OrderByElements
                .Select(item =>
                    item.SortOrder == SortOrder.Descending
                        ? (ExpressionSyntax)Invoke("Desc", this.BuildScalarExpression(item.Expression, context, wrapLiterals: false))
                        : Invoke("Asc", this.BuildScalarExpression(item.Expression, context, wrapLiterals: false)))
                .ToList();
        }

        private ExpressionSyntax BuildFrameBorderHelper(WindowDelimiter delimiter, TranspileContext context)
        {
            switch (delimiter.WindowDelimiterType)
            {
                case WindowDelimiterType.CurrentRow:
                    return MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("FrameBorder"),
                        IdentifierName("CurrentRow"));
                case WindowDelimiterType.UnboundedPreceding:
                    return MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("FrameBorder"),
                        IdentifierName("UnboundedPreceding"));
                case WindowDelimiterType.UnboundedFollowing:
                    return MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("FrameBorder"),
                        IdentifierName("UnboundedFollowing"));
                case WindowDelimiterType.ValuePreceding:
                {
                    if (delimiter.OffsetValue == null)
                    {
                        throw new SqExpressSqlTranspilerException("Window frame boundary offset cannot be empty.");
                    }

                    return InvokeMember(
                        IdentifierName("FrameBorder"),
                        "Preceding",
                        this.BuildScalarExpression(delimiter.OffsetValue, context, wrapLiterals: false));
                }
                case WindowDelimiterType.ValueFollowing:
                {
                    if (delimiter.OffsetValue == null)
                    {
                        throw new SqExpressSqlTranspilerException("Window frame boundary offset cannot be empty.");
                    }

                    return InvokeMember(
                        IdentifierName("FrameBorder"),
                        "Following",
                        this.BuildScalarExpression(delimiter.OffsetValue, context, wrapLiterals: false));
                }
                default:
                    throw new SqExpressSqlTranspilerException($"Unsupported window frame delimiter: {delimiter.WindowDelimiterType}.");
            }
        }

        private ExpressionSyntax BuildOverClause(OverClause overClause, TranspileContext context)
        {
            if (overClause.WindowName != null)
            {
                throw new SqExpressSqlTranspilerException("Named windows in OVER clause are not supported yet.");
            }

            var partitionsExpr = overClause.Partitions.Count == 0
                ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : this.BuildExprValueArray(
                    overClause.Partitions
                        .Select(partition => this.BuildScalarExpression(partition, context, wrapLiterals: false))
                        .ToList());

            var orderByExpr = overClause.OrderByClause == null
                ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : this.BuildWindowOrderBy(overClause.OrderByClause, context);

            var frameExpr = overClause.WindowFrameClause == null
                ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : this.BuildWindowFrame(overClause.WindowFrameClause, context);

            return ObjectCreationExpression(IdentifierName("ExprOver"))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList(new[]
                        {
                            Argument(partitionsExpr),
                            Argument(orderByExpr),
                            Argument(frameExpr)
                        })));
        }

        private ExpressionSyntax BuildWindowOrderBy(OrderByClause orderByClause, TranspileContext context)
        {
            if (orderByClause.OrderByElements.Count == 0)
            {
                throw new SqExpressSqlTranspilerException("OVER ORDER BY list cannot be empty.");
            }

            var items = orderByClause.OrderByElements
                .Select(item =>
                    (ExpressionSyntax)ObjectCreationExpression(IdentifierName("ExprOrderByItem"))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(new[]
                                {
                                    Argument(this.BuildScalarExpression(item.Expression, context, wrapLiterals: false)),
                                    Argument(
                                        item.SortOrder == SortOrder.Descending
                                            ? LiteralExpression(SyntaxKind.TrueLiteralExpression)
                                            : LiteralExpression(SyntaxKind.FalseLiteralExpression))
                                }))))
                .ToList();

            var itemArray = ImplicitArrayCreationExpression(
                InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SeparatedList(items)));

            return ObjectCreationExpression(IdentifierName("ExprOrderBy"))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(itemArray))));
        }

        private ExpressionSyntax BuildWindowFrame(WindowFrameClause windowFrame, TranspileContext context)
        {
            if (windowFrame.WindowFrameType == WindowFrameType.Range)
            {
                throw new SqExpressSqlTranspilerException("RANGE window frame is not supported yet. Use ROWS frame.");
            }

            var top = this.BuildWindowDelimiter(windowFrame.Top, context);
            var bottom = windowFrame.Bottom == null
                ? LiteralExpression(SyntaxKind.NullLiteralExpression)
                : this.BuildWindowDelimiter(windowFrame.Bottom, context);

            return ObjectCreationExpression(IdentifierName("ExprFrameClause"))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList(new[]
                        {
                            Argument(top),
                            Argument(bottom)
                        })));
        }

        private ExpressionSyntax BuildWindowDelimiter(WindowDelimiter delimiter, TranspileContext context)
        {
            switch (delimiter.WindowDelimiterType)
            {
                case WindowDelimiterType.CurrentRow:
                    return MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        IdentifierName("ExprCurrentRowFrameBorder"),
                        IdentifierName("Instance"));
                case WindowDelimiterType.UnboundedPreceding:
                    return ObjectCreationExpression(IdentifierName("ExprUnboundedFrameBorder"))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("FrameBorderDirection"),
                                            IdentifierName("Preceding"))))));
                case WindowDelimiterType.UnboundedFollowing:
                    return ObjectCreationExpression(IdentifierName("ExprUnboundedFrameBorder"))
                        .WithArgumentList(
                            ArgumentList(
                                SingletonSeparatedList(
                                    Argument(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("FrameBorderDirection"),
                                            IdentifierName("Following"))))));
                case WindowDelimiterType.ValuePreceding:
                {
                    if (delimiter.OffsetValue == null)
                    {
                        throw new SqExpressSqlTranspilerException("Window frame boundary offset cannot be empty.");
                    }

                    return ObjectCreationExpression(IdentifierName("ExprValueFrameBorder"))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(new[]
                                {
                                    Argument(this.BuildScalarExpression(delimiter.OffsetValue, context, wrapLiterals: false)),
                                    Argument(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("FrameBorderDirection"),
                                            IdentifierName("Preceding")))
                                })));
                }
                case WindowDelimiterType.ValueFollowing:
                {
                    if (delimiter.OffsetValue == null)
                    {
                        throw new SqExpressSqlTranspilerException("Window frame boundary offset cannot be empty.");
                    }

                    return ObjectCreationExpression(IdentifierName("ExprValueFrameBorder"))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(new[]
                                {
                                    Argument(this.BuildScalarExpression(delimiter.OffsetValue, context, wrapLiterals: false)),
                                    Argument(
                                        MemberAccessExpression(
                                            SyntaxKind.SimpleMemberAccessExpression,
                                            IdentifierName("FrameBorderDirection"),
                                            IdentifierName("Following")))
                                })));
                }
                default:
                    throw new SqExpressSqlTranspilerException($"Unsupported window frame delimiter: {delimiter.WindowDelimiterType}.");
            }
        }
    }
}
