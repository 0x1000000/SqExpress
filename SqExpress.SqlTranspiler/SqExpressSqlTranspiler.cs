using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using RoslynStatementSyntax = Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax;

namespace SqExpress.SqlTranspiler
{
    public sealed class SqExpressSqlTranspiler : ISqExpressSqlTranspiler
    {
        public SqExpressTranspileResult Transpile(string sql, SqExpressSqlTranspilerOptions? options = null)
        {
            var effectiveOptions = options ?? new SqExpressSqlTranspilerOptions();
            var script = ParseScript(sql);
            var statement = GetSingleStatement(script);

            if (statement is SelectStatement selectStatement)
            {
                return this.TranspileSelect(selectStatement, effectiveOptions);
            }

            throw new SqExpressSqlTranspilerException(
                $"Only SELECT statements are supported at the moment. Encountered: {statement.GetType().Name}.");
        }

        public SqExpressTranspileResult TranspileSelect(string sql, SqExpressSqlTranspilerOptions? options = null)
        {
            var effectiveOptions = options ?? new SqExpressSqlTranspilerOptions();
            var script = ParseScript(sql);
            var statement = GetSingleStatement(script);

            if (statement is not SelectStatement selectStatement)
            {
                throw new SqExpressSqlTranspilerException(
                    $"Expected SELECT statement but got {statement.GetType().Name}.");
            }

            return this.TranspileSelect(selectStatement, effectiveOptions);
        }

        private SqExpressTranspileResult TranspileSelect(SelectStatement selectStatement, SqExpressSqlTranspilerOptions options)
        {
            if (selectStatement.WithCtesAndXmlNamespaces != null)
            {
                throw new SqExpressSqlTranspilerException("WITH CTE/XMLNAMESPACES is not supported yet.");
            }

            if (selectStatement.QueryExpression is not QuerySpecification specification)
            {
                throw new SqExpressSqlTranspilerException("Only SELECT query specifications are supported (UNION/EXCEPT/INTERSECT are not supported yet).");
            }

            if (specification.GroupByClause != null)
            {
                throw new SqExpressSqlTranspilerException("GROUP BY is not supported yet.");
            }

            if (specification.HavingClause != null)
            {
                throw new SqExpressSqlTranspilerException("HAVING is not supported yet.");
            }

            if (selectStatement.Into != null)
            {
                throw new SqExpressSqlTranspilerException("SELECT INTO is not supported yet.");
            }

            var context = new TranspileContext();
            var queryExpression = this.BuildSelectExpression(specification, specification.OrderByClause, context);
            var doneExpression = InvokeMember(queryExpression, "Done");

            var queryVariableName = NormalizeIdentifier(options.QueryVariableName, "query");
            var bodyStatements = new List<RoslynStatementSyntax>();
            bodyStatements.AddRange(context.TableDeclarations);
            bodyStatements.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator(Identifier(queryVariableName))
                                .WithInitializer(EqualsValueClause(doneExpression)))));
            bodyStatements.Add(ReturnStatement(IdentifierName(queryVariableName)));

            var methodDeclaration = MethodDeclaration(IdentifierName("IExprQuery"), Identifier(options.MethodName))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .WithBody(Block(bodyStatements));

            var classDeclaration = ClassDeclaration(options.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddMembers(methodDeclaration);

            var compilationUnit = CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("SqExpress")),
                    UsingDirective(ParseName("SqExpress.Syntax.Select")),
                    UsingDirective(ParseName("SqExpress.SqQueryBuilder")).WithStaticKeyword(Token(SyntaxKind.StaticKeyword)))
                .AddMembers(
                    NamespaceDeclaration(ParseName(options.NamespaceName))
                        .AddMembers(classDeclaration))
                .NormalizeWhitespace();

            return new SqExpressTranspileResult(
                statementKind: "SELECT",
                ast: compilationUnit,
                cSharpCode: compilationUnit.ToFullString());
        }

        private ExpressionSyntax BuildSelectExpression(QuerySpecification specification, OrderByClause? orderByClause, TranspileContext context)
        {
            if (specification.FromClause != null)
            {
                if (specification.FromClause.TableReferences.Count != 1)
                {
                    throw new SqExpressSqlTranspilerException("Only one root FROM table-reference is supported.");
                }

                this.PreRegisterTableReferences(specification.FromClause.TableReferences[0], context);
            }

            var selectArguments = specification.SelectElements.Select(item => this.BuildSelectElement(item, context)).ToList();
            if (selectArguments.Count == 0)
            {
                throw new SqExpressSqlTranspilerException("SELECT list cannot be empty.");
            }

            var distinct = specification.UniqueRowFilter == UniqueRowFilter.Distinct;
            var top = specification.TopRowFilter;
            if (top != null && (top.Percent || top.WithTies))
            {
                throw new SqExpressSqlTranspilerException("TOP PERCENT/WITH TIES is not supported yet.");
            }

            ExpressionSyntax current;
            if (top != null)
            {
                var topExpression = UnwrapParentheses(this.BuildScalarExpression(top.Expression, context));
                var topMethod = distinct ? "SelectTopDistinct" : "SelectTop";
                current = Invoke(topMethod, Prepend(topExpression, selectArguments));
            }
            else
            {
                current = Invoke(distinct ? "SelectDistinct" : "Select", selectArguments);
            }

            if (specification.FromClause != null)
            {
                current = this.ApplyTableReference(current, specification.FromClause.TableReferences[0], context, isRoot: true);
            }

            if (specification.WhereClause != null)
            {
                var whereExpression = this.BuildBooleanExpression(specification.WhereClause.SearchCondition, context);
                current = InvokeMember(current, "Where", whereExpression);
            }

            if (orderByClause != null)
            {
                var orderByArguments = orderByClause
                    .OrderByElements
                    .Select(item => this.BuildOrderBy(item, context))
                    .ToList();

                if (orderByArguments.Count > 0)
                {
                    current = InvokeMember(current, "OrderBy", orderByArguments);
                }
            }

            return current;
        }

        private void PreRegisterTableReferences(TableReference tableReference, TranspileContext context)
        {
            if (tableReference is NamedTableReference named)
            {
                context.GetOrAddNamedTable(named);
                return;
            }

            if (tableReference is JoinParenthesisTableReference joinParenthesis)
            {
                this.PreRegisterTableReferences(joinParenthesis.Join, context);
                return;
            }

            if (tableReference is QualifiedJoin qualifiedJoin)
            {
                this.PreRegisterTableReferences(qualifiedJoin.FirstTableReference, context);
                this.PreRegisterTableReferences(qualifiedJoin.SecondTableReference, context);
                return;
            }

            if (tableReference is UnqualifiedJoin unqualifiedJoin)
            {
                this.PreRegisterTableReferences(unqualifiedJoin.FirstTableReference, context);
                this.PreRegisterTableReferences(unqualifiedJoin.SecondTableReference, context);
                return;
            }

            throw new SqExpressSqlTranspilerException($"Unsupported table reference: {tableReference.GetType().Name}.");
        }

        private ExpressionSyntax BuildSelectElement(SelectElement selectElement, TranspileContext context)
        {
            if (selectElement is SelectScalarExpression scalar)
            {
                var expression = this.BuildScalarExpression(scalar.Expression, context);
                var alias = TryGetSelectAlias(scalar.ColumnName);
                if (alias != null)
                {
                    return InvokeMember(expression, "As", StringLiteral(alias));
                }

                return expression;
            }

            if (selectElement is SelectStarExpression star)
            {
                if (star.Qualifier == null || star.Qualifier.Identifiers.Count == 0)
                {
                    return Invoke("AllColumns");
                }

                var sourceName = star.Qualifier.Identifiers[star.Qualifier.Identifiers.Count - 1].Value;
                if (!context.TryResolveTableVariable(sourceName, out var variableName))
                {
                    throw new SqExpressSqlTranspilerException($"Could not resolve SELECT * qualifier '{sourceName}'.");
                }

                return InvokeMember(IdentifierName(variableName), "AllColumns");
            }

            throw new SqExpressSqlTranspilerException($"Unsupported select element: {selectElement.GetType().Name}.");
        }

        private ExpressionSyntax ApplyTableReference(ExpressionSyntax current, TableReference tableReference, TranspileContext context, bool isRoot)
        {
            if (tableReference is NamedTableReference namedTable)
            {
                if (!isRoot)
                {
                    throw new SqExpressSqlTranspilerException("Unexpected standalone table reference in joined table tree.");
                }

                var tableVariable = context.GetOrAddNamedTable(namedTable);
                return InvokeMember(current, "From", IdentifierName(tableVariable));
            }

            if (tableReference is JoinParenthesisTableReference joinParenthesis)
            {
                return this.ApplyTableReference(current, joinParenthesis.Join, context, isRoot);
            }

            if (tableReference is QualifiedJoin qualifiedJoin)
            {
                var withLeft = this.ApplyTableReference(current, qualifiedJoin.FirstTableReference, context, isRoot);
                var rightTableVariable = this.ResolveJoinedTableVariable(qualifiedJoin.SecondTableReference, context);
                var onExpression = this.BuildBooleanExpression(qualifiedJoin.SearchCondition, context);

                string joinMethod = qualifiedJoin.QualifiedJoinType switch
                {
                    QualifiedJoinType.Inner => "InnerJoin",
                    QualifiedJoinType.LeftOuter => "LeftJoin",
                    QualifiedJoinType.FullOuter => "FullJoin",
                    QualifiedJoinType.RightOuter => throw new SqExpressSqlTranspilerException("RIGHT JOIN is not supported yet."),
                    _ => throw new SqExpressSqlTranspilerException($"Unsupported qualified join type: {qualifiedJoin.QualifiedJoinType}.")
                };

                return InvokeMember(withLeft, joinMethod, IdentifierName(rightTableVariable), onExpression);
            }

            if (tableReference is UnqualifiedJoin unqualifiedJoin)
            {
                var withLeft = this.ApplyTableReference(current, unqualifiedJoin.FirstTableReference, context, isRoot);
                var rightTableVariable = this.ResolveJoinedTableVariable(unqualifiedJoin.SecondTableReference, context);

                return unqualifiedJoin.UnqualifiedJoinType switch
                {
                    UnqualifiedJoinType.CrossJoin => InvokeMember(withLeft, "CrossJoin", IdentifierName(rightTableVariable)),
                    _ => throw new SqExpressSqlTranspilerException($"Unsupported unqualified join type: {unqualifiedJoin.UnqualifiedJoinType}.")
                };
            }

            throw new SqExpressSqlTranspilerException($"Unsupported table reference: {tableReference.GetType().Name}.");
        }

        private string ResolveJoinedTableVariable(TableReference tableReference, TranspileContext context)
        {
            if (tableReference is NamedTableReference namedTable)
            {
                return context.GetOrAddNamedTable(namedTable);
            }

            if (tableReference is JoinParenthesisTableReference parenthesized)
            {
                return this.ResolveJoinedTableVariable(parenthesized.Join, context);
            }

            throw new SqExpressSqlTranspilerException("Only named table references are supported on the right side of JOIN.");
        }

        private ExpressionSyntax BuildBooleanExpression(BooleanExpression expression, TranspileContext context)
        {
            if (expression is BooleanParenthesisExpression parenthesis)
            {
                return ParenthesizedExpression(this.BuildBooleanExpression(parenthesis.Expression, context));
            }

            if (expression is BooleanNotExpression notExpression)
            {
                return PrefixUnaryExpression(
                    SyntaxKind.LogicalNotExpression,
                    ParenthesizeIfNeeded(this.BuildBooleanExpression(notExpression.Expression, context)));
            }

            if (expression is BooleanBinaryExpression binary)
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

            if (expression is BooleanComparisonExpression comparison)
            {
                var kind = MapComparisonKind(comparison.ComparisonType);
                return BinaryExpression(
                    kind,
                    ParenthesizeIfNeeded(this.BuildScalarExpression(comparison.FirstExpression, context)),
                    ParenthesizeIfNeeded(this.BuildScalarExpression(comparison.SecondExpression, context)));
            }

            if (expression is BooleanIsNullExpression isNull)
            {
                var test = this.BuildScalarExpression(isNull.Expression, context);
                return Invoke(isNull.IsNot ? "IsNotNull" : "IsNull", test);
            }

            if (expression is InPredicate inPredicate)
            {
                if (inPredicate.Subquery != null)
                {
                    throw new SqExpressSqlTranspilerException("IN (subquery) is not supported yet.");
                }

                if (inPredicate.Values.Count < 1)
                {
                    throw new SqExpressSqlTranspilerException("IN predicate cannot be empty.");
                }

                if (inPredicate.Expression is not ColumnReferenceExpression inColumn)
                {
                    throw new SqExpressSqlTranspilerException("IN predicate is supported only for column references.");
                }

                var column = this.BuildColumnExpression(inColumn, context);
                var values = inPredicate.Values.Select(item => this.BuildScalarExpression(item, context)).ToList();
                var inCall = InvokeMember(column, "In", values);
                if (inPredicate.NotDefined)
                {
                    return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizeIfNeeded(inCall));
                }

                return inCall;
            }

            if (expression is LikePredicate like)
            {
                if (like.SecondExpression is not StringLiteral stringPattern)
                {
                    throw new SqExpressSqlTranspilerException("LIKE is supported only with string literal pattern.");
                }

                var test = this.BuildScalarExpression(like.FirstExpression, context);
                var likeCall = Invoke("Like", test, StringLiteral(stringPattern.Value));
                if (like.NotDefined)
                {
                    return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizeIfNeeded(likeCall));
                }

                return likeCall;
            }

            if (expression is BooleanTernaryExpression between)
            {
                if (between.TernaryExpressionType != BooleanTernaryExpressionType.Between
                    && between.TernaryExpressionType != BooleanTernaryExpressionType.NotBetween)
                {
                    throw new SqExpressSqlTranspilerException($"Unsupported boolean ternary expression: {between.TernaryExpressionType}.");
                }

                var test = this.BuildScalarExpression(between.FirstExpression, context);
                var start = this.BuildScalarExpression(between.SecondExpression, context);
                var end = this.BuildScalarExpression(between.ThirdExpression, context);

                var betweenExpression =
                    BinaryExpression(
                        SyntaxKind.BitwiseAndExpression,
                        ParenthesizedExpression(BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression, test, start)),
                        ParenthesizedExpression(BinaryExpression(SyntaxKind.LessThanOrEqualExpression, test, end)));

                if (between.TernaryExpressionType == BooleanTernaryExpressionType.NotBetween)
                {
                    return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizeIfNeeded(betweenExpression));
                }

                return betweenExpression;
            }

            throw new SqExpressSqlTranspilerException($"Unsupported boolean expression: {expression.GetType().Name}.");
        }

        private ExpressionSyntax BuildScalarExpression(ScalarExpression expression, TranspileContext context)
        {
            if (expression is ColumnReferenceExpression columnReference)
            {
                return this.BuildColumnExpression(columnReference, context);
            }

            if (expression is NullLiteral)
            {
                return IdentifierName("Null");
            }

            if (expression is StringLiteral stringLiteral)
            {
                return Invoke("Literal", StringLiteral(stringLiteral.Value));
            }

            if (expression is IntegerLiteral integerLiteral)
            {
                return Invoke("Literal", NumericLiteral(integerLiteral.Value));
            }

            if (expression is NumericLiteral numericLiteral)
            {
                return Invoke("Literal", DecimalOrDoubleLiteral(numericLiteral.Value));
            }

            if (expression is MoneyLiteral moneyLiteral)
            {
                return Invoke("Literal", DecimalOrDoubleLiteral(moneyLiteral.Value));
            }

            if (expression is UnaryExpression unary)
            {
                var unaryKind = unary.UnaryExpressionType switch
                {
                    UnaryExpressionType.Negative => SyntaxKind.UnaryMinusExpression,
                    UnaryExpressionType.Positive => SyntaxKind.UnaryPlusExpression,
                    UnaryExpressionType.BitwiseNot => SyntaxKind.BitwiseNotExpression,
                    _ => throw new SqExpressSqlTranspilerException($"Unsupported unary expression: {unary.UnaryExpressionType}.")
                };

                return PrefixUnaryExpression(unaryKind, ParenthesizeIfNeeded(this.BuildScalarExpression(unary.Expression, context)));
            }

            if (expression is ParenthesisExpression parenthesis)
            {
                return ParenthesizedExpression(this.BuildScalarExpression(parenthesis.Expression, context));
            }

            if (expression is BinaryExpression binary)
            {
                var binaryKind = MapBinaryKind(binary.BinaryExpressionType);
                return BinaryExpression(
                    binaryKind,
                    ParenthesizeIfNeeded(this.BuildScalarExpression(binary.FirstExpression, context)),
                    ParenthesizeIfNeeded(this.BuildScalarExpression(binary.SecondExpression, context)));
            }

            if (expression is FunctionCall functionCall)
            {
                return this.BuildFunctionCall(functionCall, context);
            }

            if (expression is CastCall castCall)
            {
                return Invoke("Cast", this.BuildScalarExpression(castCall.Parameter, context), this.BuildSqlType(castCall.DataType));
            }

            throw new SqExpressSqlTranspilerException($"Unsupported scalar expression: {expression.GetType().Name}.");
        }

        private ExpressionSyntax BuildSqlType(DataTypeReference dataType)
        {
            if (dataType is not SqlDataTypeReference sqlDataType)
            {
                throw new SqExpressSqlTranspilerException($"Unsupported SQL type expression: {dataType.GetType().Name}.");
            }

            return sqlDataType.SqlDataTypeOption switch
            {
                SqlDataTypeOption.Int => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Int32")),
                SqlDataTypeOption.BigInt => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Int64")),
                SqlDataTypeOption.SmallInt => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Int16")),
                SqlDataTypeOption.Bit => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Boolean")),
                SqlDataTypeOption.Decimal => InvokeMember(IdentifierName("SqlType"), "Decimal"),
                SqlDataTypeOption.Numeric => InvokeMember(IdentifierName("SqlType"), "Decimal"),
                SqlDataTypeOption.Float => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Double")),
                SqlDataTypeOption.DateTime => InvokeMember(IdentifierName("SqlType"), "DateTime"),
                SqlDataTypeOption.Date => InvokeMember(IdentifierName("SqlType"), "DateTime", LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                SqlDataTypeOption.UniqueIdentifier => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Guid")),
                SqlDataTypeOption.VarChar => InvokeMember(IdentifierName("SqlType"), "String"),
                SqlDataTypeOption.NVarChar => InvokeMember(IdentifierName("SqlType"), "String", LiteralExpression(SyntaxKind.NullLiteralExpression), LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                _ => throw new SqExpressSqlTranspilerException($"Unsupported SQL cast type: {sqlDataType.SqlDataTypeOption}.")
            };
        }

        private ExpressionSyntax BuildFunctionCall(FunctionCall functionCall, TranspileContext context)
        {
            if (functionCall.CallTarget != null)
            {
                throw new SqExpressSqlTranspilerException("Schema-qualified function calls are not supported yet.");
            }

            var functionName = functionCall.FunctionName.Value;
            if (string.Equals(functionName, "COUNT", StringComparison.OrdinalIgnoreCase))
            {
                if (functionCall.Parameters.Count == 1 && IsStar(functionCall.Parameters[0]))
                {
                    if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct)
                    {
                        throw new SqExpressSqlTranspilerException("COUNT(DISTINCT *) is not valid.");
                    }

                    return Invoke("CountOne");
                }

                if (functionCall.Parameters.Count != 1)
                {
                    throw new SqExpressSqlTranspilerException("COUNT supports exactly one argument.");
                }

                var countArg = this.BuildScalarExpression(functionCall.Parameters[0], context);
                return functionCall.UniqueRowFilter == UniqueRowFilter.Distinct
                    ? Invoke("CountDistinct", countArg)
                    : Invoke("Count", countArg);
            }

            if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct)
            {
                if (functionCall.Parameters.Count != 1)
                {
                    throw new SqExpressSqlTranspilerException("DISTINCT aggregate function with multiple arguments is not supported.");
                }

                return Invoke("AggregateFunction", StringLiteral(functionName), LiteralExpression(SyntaxKind.TrueLiteralExpression), this.BuildScalarExpression(functionCall.Parameters[0], context));
            }

            var functionArgs = functionCall.Parameters.Select(arg =>
            {
                if (IsStar(arg))
                {
                    throw new SqExpressSqlTranspilerException($"Function '{functionName}' with '*' argument is not supported.");
                }
                return this.BuildScalarExpression(arg, context);
            }).ToList();

            return Invoke("ScalarFunctionSys", Prepend(StringLiteral(functionName), functionArgs));
        }

        private ExpressionSyntax BuildColumnExpression(ColumnReferenceExpression columnReference, TranspileContext context)
        {
            if (columnReference.ColumnType == ColumnType.Wildcard)
            {
                throw new SqExpressSqlTranspilerException("Wildcard column reference cannot be used as scalar expression.");
            }

            var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
            if (identifiers == null || identifiers.Count < 1)
            {
                throw new SqExpressSqlTranspilerException("Column reference does not contain identifiers.");
            }

            var columnName = identifiers[identifiers.Count - 1].Value;
            if (identifiers.Count == 1)
            {
                return Invoke("Column", StringLiteral(columnName));
            }

            for (int i = identifiers.Count - 2; i >= 0; i--)
            {
                var sourceName = identifiers[i].Value;
                if (context.TryResolveTableVariable(sourceName, out var tableVariableName))
                {
                    return InvokeMember(IdentifierName(tableVariableName), "Column", StringLiteral(columnName));
                }
            }

            return Invoke("Column", StringLiteral(columnName));
        }

        private ExpressionSyntax BuildOrderBy(ExpressionWithSortOrder orderByItem, TranspileContext context)
        {
            var expression = this.BuildScalarExpression(orderByItem.Expression, context);
            if (orderByItem.SortOrder == SortOrder.Descending)
            {
                return Invoke("Desc", expression);
            }

            return expression;
        }

        private static SyntaxKind MapBinaryKind(BinaryExpressionType binaryExpressionType)
        {
            return binaryExpressionType switch
            {
                BinaryExpressionType.Add => SyntaxKind.AddExpression,
                BinaryExpressionType.Subtract => SyntaxKind.SubtractExpression,
                BinaryExpressionType.Multiply => SyntaxKind.MultiplyExpression,
                BinaryExpressionType.Divide => SyntaxKind.DivideExpression,
                BinaryExpressionType.Modulo => SyntaxKind.ModuloExpression,
                BinaryExpressionType.BitwiseAnd => SyntaxKind.BitwiseAndExpression,
                BinaryExpressionType.BitwiseOr => SyntaxKind.BitwiseOrExpression,
                BinaryExpressionType.BitwiseXor => SyntaxKind.ExclusiveOrExpression,
                _ => throw new SqExpressSqlTranspilerException($"Unsupported scalar binary operation: {binaryExpressionType}.")
            };
        }

        private static SyntaxKind MapComparisonKind(BooleanComparisonType comparisonType)
        {
            return comparisonType switch
            {
                BooleanComparisonType.Equals => SyntaxKind.EqualsExpression,
                BooleanComparisonType.GreaterThan => SyntaxKind.GreaterThanExpression,
                BooleanComparisonType.LessThan => SyntaxKind.LessThanExpression,
                BooleanComparisonType.GreaterThanOrEqualTo => SyntaxKind.GreaterThanOrEqualExpression,
                BooleanComparisonType.LessThanOrEqualTo => SyntaxKind.LessThanOrEqualExpression,
                BooleanComparisonType.NotEqualToBrackets => SyntaxKind.NotEqualsExpression,
                BooleanComparisonType.NotEqualToExclamation => SyntaxKind.NotEqualsExpression,
                BooleanComparisonType.NotLessThan => SyntaxKind.GreaterThanOrEqualExpression,
                BooleanComparisonType.NotGreaterThan => SyntaxKind.LessThanOrEqualExpression,
                _ => throw new SqExpressSqlTranspilerException($"Unsupported comparison operation: {comparisonType}.")
            };
        }

        private static bool IsStar(ScalarExpression expression)
            => expression is ColumnReferenceExpression column && column.ColumnType == ColumnType.Wildcard;

        private static TSqlScript ParseScript(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new SqExpressSqlTranspilerException("SQL text cannot be empty.");
            }

            var parser = new TSql160Parser(initialQuotedIdentifiers: true);
            using var reader = new StringReader(sql);
            var fragment = parser.Parse(reader, out var errors);
            if (errors.Count > 0)
            {
                var details = string.Join(
                    Environment.NewLine,
                    errors.Select(e => $"({e.Line},{e.Column}) {e.Message}"));
                throw new SqExpressSqlTranspilerException($"Could not parse SQL:{Environment.NewLine}{details}");
            }

            if (fragment is not TSqlScript script)
            {
                throw new SqExpressSqlTranspilerException($"Unexpected parser root node: {fragment.GetType().Name}.");
            }

            return script;
        }

        private static TSqlStatement GetSingleStatement(TSqlScript script)
        {
            if (script.Batches.Count != 1)
            {
                throw new SqExpressSqlTranspilerException("Only one SQL batch is supported.");
            }

            var batch = script.Batches[0];
            if (batch.Statements.Count != 1)
            {
                throw new SqExpressSqlTranspilerException("Only one SQL statement is supported.");
            }

            return batch.Statements[0];
        }

        private static string? TryGetSelectAlias(IdentifierOrValueExpression? alias)
        {
            if (alias == null)
            {
                return null;
            }

            if (alias.Identifier != null)
            {
                return alias.Identifier.Value;
            }

            if (alias.Value != null)
            {
                return alias.Value;
            }

            return null;
        }

        private static ExpressionSyntax ParenthesizeIfNeeded(ExpressionSyntax expression)
            => expression is BinaryExpressionSyntax || expression is PrefixUnaryExpressionSyntax
                ? ParenthesizedExpression(expression)
                : expression;

        private static ExpressionSyntax UnwrapParentheses(ExpressionSyntax expression)
        {
            var current = expression;
            while (current is ParenthesizedExpressionSyntax wrapped)
            {
                current = wrapped.Expression;
            }

            return current;
        }

        private static ExpressionSyntax Invoke(string methodName, params ExpressionSyntax[] arguments)
            => Invoke(methodName, (IReadOnlyList<ExpressionSyntax>)arguments);

        private static ExpressionSyntax Invoke(string methodName, IReadOnlyList<ExpressionSyntax> arguments)
            => InvocationExpression(
                IdentifierName(methodName),
                ArgumentList(SeparatedList(arguments.Select(Argument))));

        private static ExpressionSyntax InvokeMember(ExpressionSyntax target, string methodName, params ExpressionSyntax[] arguments)
            => InvokeMember(target, methodName, (IReadOnlyList<ExpressionSyntax>)arguments);

        private static ExpressionSyntax InvokeMember(ExpressionSyntax target, string methodName, IReadOnlyList<ExpressionSyntax> arguments)
            => InvocationExpression(
                MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, target, IdentifierName(methodName)),
                ArgumentList(SeparatedList(arguments.Select(Argument))));

        private static ExpressionSyntax StringLiteral(string value)
            => LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(value));

        private static ExpressionSyntax NumericLiteral(string value)
        {
            if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
            {
                if (longValue <= int.MaxValue && longValue >= int.MinValue)
                {
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal((int)longValue));
                }

                return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(longValue));
            }

            throw new SqExpressSqlTranspilerException($"Could not parse integer literal '{value}'.");
        }

        private static ExpressionSyntax DecimalOrDoubleLiteral(string value)
        {
            if (decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var decimalValue))
            {
                return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(decimalValue));
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
            {
                return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(doubleValue));
            }

            throw new SqExpressSqlTranspilerException($"Could not parse numeric literal '{value}'.");
        }

        private static IReadOnlyList<ExpressionSyntax> Prepend(ExpressionSyntax first, IReadOnlyList<ExpressionSyntax> rest)
        {
            var result = new List<ExpressionSyntax>(rest.Count + 1) { first };
            result.AddRange(rest);
            return result;
        }

        private static string NormalizeIdentifier(string name, string fallback)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return fallback;
            }

            var chars = name.Where(ch => char.IsLetterOrDigit(ch) || ch == '_').ToArray();
            var cleaned = chars.Length == 0 ? fallback : new string(chars);
            if (char.IsDigit(cleaned[0]) || SyntaxFacts.GetKeywordKind(cleaned) != SyntaxKind.None)
            {
                cleaned = "_" + cleaned;
            }

            return cleaned;
        }

        private sealed class TranspileContext
        {
            private readonly List<LocalDeclarationStatementSyntax> _tableDeclarations = new();
            private readonly Dictionary<string, string> _tableAliasMap = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, string?> _tableNameMap = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _variableNames = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<NamedTableReference, string> _tableReferenceMap = new(ReferenceEqualityComparer<NamedTableReference>.Instance);

            public IReadOnlyList<LocalDeclarationStatementSyntax> TableDeclarations => this._tableDeclarations;

            public string GetOrAddNamedTable(NamedTableReference table)
            {
                if (this._tableReferenceMap.TryGetValue(table, out var byReference))
                {
                    return byReference;
                }

                if (table.SchemaObject == null)
                {
                    throw new SqExpressSqlTranspilerException("Only schema object table references are supported.");
                }

                if (table.SchemaObject.ServerIdentifier != null)
                {
                    throw new SqExpressSqlTranspilerException("Server-qualified table names are not supported.");
                }

                var tableName = table.SchemaObject.BaseIdentifier?.Value;
                if (string.IsNullOrWhiteSpace(tableName))
                {
                    throw new SqExpressSqlTranspilerException("Table name is missing.");
                }
                var tableNameNotNull = tableName!;

                var alias = table.Alias?.Value;
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    var aliasKey = alias!;
                    if (this._tableAliasMap.TryGetValue(aliasKey, out var existingByAlias))
                    {
                        this._tableReferenceMap[table] = existingByAlias;
                        return existingByAlias;
                    }
                }

                var variableName = this.CreateVariableName(alias ?? tableNameNotNull);
                this._tableDeclarations.Add(CreateTableDeclaration(variableName, tableNameNotNull, table.SchemaObject.SchemaIdentifier?.Value, table.SchemaObject.DatabaseIdentifier?.Value, alias));

                if (!string.IsNullOrWhiteSpace(alias))
                {
                    this._tableAliasMap[alias!] = variableName;
                }

                if (this._tableNameMap.TryGetValue(tableNameNotNull, out var existingByName))
                {
                    if (!string.Equals(existingByName, variableName, StringComparison.OrdinalIgnoreCase))
                    {
                        this._tableNameMap[tableNameNotNull] = null;
                    }
                }
                else
                {
                    this._tableNameMap[tableNameNotNull] = variableName;
                }

                this._tableReferenceMap[table] = variableName;
                return variableName;
            }

            public bool TryResolveTableVariable(string sourceName, out string tableVariableName)
            {
                if (this._tableAliasMap.TryGetValue(sourceName, out var aliasTableVariableName))
                {
                    tableVariableName = aliasTableVariableName;
                    return true;
                }

                if (this._tableNameMap.TryGetValue(sourceName, out var variableName) && variableName != null)
                {
                    tableVariableName = variableName;
                    return true;
                }

                tableVariableName = string.Empty;
                return false;
            }

            private string CreateVariableName(string source)
            {
                var normalized = NormalizeIdentifier(source, "t");
                normalized = char.ToLowerInvariant(normalized[0]) + normalized.Substring(1);

                var candidate = normalized;
                var index = 0;
                while (this._variableNames.Contains(candidate))
                {
                    index++;
                    candidate = normalized + index.ToString(CultureInfo.InvariantCulture);
                }

                this._variableNames.Add(candidate);
                return candidate;
            }

            private static LocalDeclarationStatementSyntax CreateTableDeclaration(string variableName, string tableName, string? schema, string? database, string? alias)
            {
                var arguments = new List<ExpressionSyntax>();
                if (database != null)
                {
                    if (schema == null)
                    {
                        throw new SqExpressSqlTranspilerException("Database-qualified table without schema is not supported.");
                    }

                    arguments.Add(StringLiteral(database));
                    arguments.Add(StringLiteral(schema));
                    arguments.Add(StringLiteral(tableName));
                }
                else
                {
                    arguments.Add(schema != null ? StringLiteral(schema) : LiteralExpression(SyntaxKind.NullLiteralExpression));
                    arguments.Add(StringLiteral(tableName));
                }

                if (!string.IsNullOrWhiteSpace(alias))
                {
                    arguments.Add(StringLiteral(alias!));
                }

                return LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator(Identifier(variableName))
                                .WithInitializer(
                                    EqualsValueClause(
                                        ObjectCreationExpression(IdentifierName("TableBase"))
                                            .WithArgumentList(ArgumentList(SeparatedList(arguments.Select(Argument))))))));
            }

            private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
                where T : class
            {
                public static readonly ReferenceEqualityComparer<T> Instance = new ReferenceEqualityComparer<T>();

                public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

                public int GetHashCode(T obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
            }
        }
    }
}
