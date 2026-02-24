using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SqExpress.SqlTranspiler
{
    public sealed partial class SqExpressSqlTranspiler
    {
        private ExpressionSyntax BuildBooleanExpression(BooleanExpression expression, TranspileContext context)
        {
            switch (expression)
            {
                case BooleanParenthesisExpression parenthesis:
                    return ParenthesizedExpression(this.BuildBooleanExpression(parenthesis.Expression, context));
                case BooleanNotExpression notExpression:
                    return PrefixUnaryExpression(
                        SyntaxKind.LogicalNotExpression,
                        ParenthesizeIfNeeded(this.BuildBooleanExpression(notExpression.Expression, context)));
                case BooleanBinaryExpression binary:
                    return this.BuildBooleanBinaryExpression(binary, context);
                case BooleanComparisonExpression comparison:
                    return this.BuildBooleanComparisonExpression(comparison, context);
                case BooleanIsNullExpression isNull:
                    return this.BuildBooleanIsNullExpression(isNull, context);
                case InPredicate inPredicate:
                    return this.BuildInPredicateExpression(inPredicate, context);
                case ExistsPredicate existsPredicate:
                    return Invoke("Exists", this.BuildSubQueryExpression(existsPredicate.Subquery, context));
                case LikePredicate like:
                    return this.BuildLikePredicateExpression(like, context);
                case BooleanTernaryExpression between:
                    return this.BuildBetweenExpression(between, context);
                default:
                    throw new SqExpressSqlTranspilerException($"Unsupported boolean expression: {expression.GetType().Name}.");
            }
        }

        private ExpressionSyntax BuildBooleanBinaryExpression(BooleanBinaryExpression binary, TranspileContext context)
        {
            var kind = binary.BinaryExpressionType switch
            {
                BooleanBinaryExpressionType.And => SyntaxKind.BitwiseAndExpression,
                BooleanBinaryExpressionType.Or => SyntaxKind.BitwiseOrExpression,
                _ => throw new SqExpressSqlTranspilerException($"Unsupported boolean binary operation: {binary.BinaryExpressionType}.")
            };

            return BinaryExpression(
                kind,
                ParenthesizeIfNeeded(this.BuildBooleanExpression(binary.FirstExpression, context)),
                ParenthesizeIfNeeded(this.BuildBooleanExpression(binary.SecondExpression, context)));
        }

        private ExpressionSyntax BuildBooleanComparisonExpression(BooleanComparisonExpression comparison, TranspileContext context)
        {
            this.RegisterComparisonVariableHints(comparison, context);
            this.ApplyComparisonColumnHints(comparison, context);

            var kind = MapComparisonKind(comparison.ComparisonType);
            var leftExpression = this.BuildComparisonOperand(comparison.FirstExpression, comparison.SecondExpression, context);
            var rightExpression = this.BuildComparisonOperand(comparison.SecondExpression, comparison.FirstExpression, context);
            return BinaryExpression(
                kind,
                ParenthesizeIfNeeded(leftExpression),
                ParenthesizeIfNeeded(rightExpression));
        }

        private void RegisterComparisonVariableHints(BooleanComparisonExpression comparison, TranspileContext context)
        {
            if (this.TryExtractVariableReference(comparison.FirstExpression, out var firstVariableName, out var firstVariableHint))
            {
                context.RegisterSqlVariable(firstVariableName, firstVariableHint);
                context.RegisterSqlVariable(firstVariableName, this.InferScalarVariableKind(comparison.SecondExpression, context));
            }

            if (this.TryExtractVariableReference(comparison.SecondExpression, out var secondVariableName, out var secondVariableHint))
            {
                context.RegisterSqlVariable(secondVariableName, secondVariableHint);
                context.RegisterSqlVariable(secondVariableName, this.InferScalarVariableKind(comparison.FirstExpression, context));
            }
        }

        private void ApplyComparisonColumnHints(BooleanComparisonExpression comparison, TranspileContext context)
        {
            if (comparison.SecondExpression is ColumnReferenceExpression rightColumn
                && this.TryInferDescriptorColumnKind(comparison.FirstExpression, context, out var rightHint))
            {
                context.MarkColumnAsKind(rightColumn, rightHint);
            }

            if (comparison.FirstExpression is ColumnReferenceExpression leftColumn
                && this.TryInferDescriptorColumnKind(comparison.SecondExpression, context, out var leftHint))
            {
                context.MarkColumnAsKind(leftColumn, leftHint);
            }
        }

        private ExpressionSyntax BuildBooleanIsNullExpression(BooleanIsNullExpression isNull, TranspileContext context)
        {
            if (isNull.Expression is ColumnReferenceExpression nullableColumn)
            {
                context.MarkColumnNullable(nullableColumn);
            }

            var test = this.BuildScalarExpression(isNull.Expression, context, wrapLiterals: false);
            return Invoke(isNull.IsNot ? "IsNotNull" : "IsNull", test);
        }

        private ExpressionSyntax BuildInPredicateExpression(InPredicate inPredicate, TranspileContext context)
        {
            if (inPredicate.Expression is not ColumnReferenceExpression inColumn)
            {
                throw new SqExpressSqlTranspilerException("IN predicate is supported only for column references.");
            }

            var inCall = this.BuildInPredicateCall(inPredicate, inColumn, context);
            return inPredicate.NotDefined
                ? PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizeIfNeeded(inCall))
                : inCall;
        }

        private ExpressionSyntax BuildInPredicateCall(InPredicate inPredicate, ColumnReferenceExpression inColumn, TranspileContext context)
        {
            var column = this.BuildColumnExpression(inColumn, context);
            if (inPredicate.Subquery != null)
            {
                var inSubQuery = this.BuildSubQueryExpression(inPredicate.Subquery, context);
                return InvokeMember(column, "In", inSubQuery);
            }

            if (inPredicate.Values.Count < 1)
            {
                throw new SqExpressSqlTranspilerException("IN predicate cannot be empty.");
            }

            if (inPredicate.Values.Count == 1 && inPredicate.Values[0] is VariableReference listVariable)
            {
                var listKind = this.InferListVariableKind(inColumn, context);
                var registeredListVariable = context.RegisterSqlVariable(listVariable.Name, listKind);
                return InvokeMember(column, "In", IdentifierName(registeredListVariable.VariableName));
            }

            foreach (var value in inPredicate.Values)
            {
                if (this.TryInferDescriptorColumnKind(value, context, out var inHint))
                {
                    context.MarkColumnAsKind(inColumn, inHint);
                }
            }

            var values = inPredicate.Values.Select(item => this.BuildScalarExpression(item, context, wrapLiterals: false)).ToList();
            return InvokeMember(column, "In", values);
        }

        private ExpressionSyntax BuildLikePredicateExpression(LikePredicate like, TranspileContext context)
        {
            if (like.SecondExpression is not StringLiteral stringPattern)
            {
                throw new SqExpressSqlTranspilerException("LIKE is supported only with string literal pattern.");
            }

            if (like.FirstExpression is ColumnReferenceExpression likeColumn)
            {
                context.MarkColumnAsKind(likeColumn, DescriptorColumnKind.NVarChar);
            }

            var test = this.BuildScalarExpression(like.FirstExpression, context, wrapLiterals: false);
            var likeCall = Invoke("Like", test, StringLiteral(stringPattern.Value));
            return like.NotDefined
                ? PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizeIfNeeded(likeCall))
                : likeCall;
        }

        private ExpressionSyntax BuildBetweenExpression(BooleanTernaryExpression between, TranspileContext context)
        {
            if (between.TernaryExpressionType != BooleanTernaryExpressionType.Between
                && between.TernaryExpressionType != BooleanTernaryExpressionType.NotBetween)
            {
                throw new SqExpressSqlTranspilerException($"Unsupported boolean ternary expression: {between.TernaryExpressionType}.");
            }

            if (between.FirstExpression is ColumnReferenceExpression betweenColumn)
            {
                if (this.TryInferDescriptorColumnKind(between.SecondExpression, context, out var betweenSecondHint))
                {
                    context.MarkColumnAsKind(betweenColumn, betweenSecondHint);
                }

                if (this.TryInferDescriptorColumnKind(between.ThirdExpression, context, out var betweenThirdHint))
                {
                    context.MarkColumnAsKind(betweenColumn, betweenThirdHint);
                }
            }

            var test = this.BuildScalarExpression(between.FirstExpression, context, wrapLiterals: false);
            var start = this.BuildScalarExpression(between.SecondExpression, context, wrapLiterals: false);
            var end = this.BuildScalarExpression(between.ThirdExpression, context, wrapLiterals: false);

            var betweenExpression =
                BinaryExpression(
                    SyntaxKind.BitwiseAndExpression,
                    ParenthesizedExpression(BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, test, start)),
                    ParenthesizedExpression(BinaryExpression(SyntaxKind.LessThanOrEqualExpression, test, end)));

            return between.TernaryExpressionType == BooleanTernaryExpressionType.NotBetween
                ? PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizeIfNeeded(betweenExpression))
                : betweenExpression;
        }

        private ExpressionSyntax BuildComparisonOperand(ScalarExpression operand, ScalarExpression opposite, TranspileContext context)
        {
            if (operand is VariableReference variableReference
                && opposite is ColumnReferenceExpression)
            {
                var variable = context.RegisterSqlVariable(variableReference.Name, SqlVariableKind.UnknownScalar);
                return IdentifierName(variable.VariableName);
            }

            return this.BuildScalarExpression(operand, context, wrapLiterals: false);
        }
    }
}
