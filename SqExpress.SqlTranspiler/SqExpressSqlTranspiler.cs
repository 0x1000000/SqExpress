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

            var context = new TranspileContext(options);
            context.RegisterCtes(selectStatement.WithCtesAndXmlNamespaces);

            if (specification.FromClause != null)
            {
                if (specification.FromClause.TableReferences.Count != 1)
                {
                    throw new SqExpressSqlTranspilerException("Only one root FROM table-reference is supported.");
                }

                this.PreRegisterTableReferences(specification.FromClause.TableReferences[0], context);
            }

            var queryExpression = this.BuildSelectExpression(specification, specification.OrderByClause, context);
            var doneExpression = InvokeMember(queryExpression, "Done");

            var queryAst = this.BuildQueryAst(doneExpression, context, options);
            var declarationsAst = this.BuildDeclarationsAst(context, options);

            return new SqExpressTranspileResult(
                statementKind: "SELECT",
                queryAst: queryAst,
                declarationsAst: declarationsAst);
        }

        private CompilationUnitSyntax BuildQueryAst(ExpressionSyntax doneExpression, TranspileContext context, SqExpressSqlTranspilerOptions options)
        {
            var queryVariableName = NormalizeIdentifier(options.QueryVariableName, "query");
            var bodyStatements = new List<RoslynStatementSyntax>();
            bodyStatements.AddRange(context.SourceDeclarations);
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

            return CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("SqExpress")),
                    UsingDirective(ParseName("SqExpress.Syntax.Select")),
                    UsingDirective(ParseName(options.EffectiveDeclarationsNamespaceName)),
                    UsingDirective(ParseName("SqExpress.SqQueryBuilder")).WithStaticKeyword(Token(SyntaxKind.StaticKeyword)))
                .AddMembers(
                    NamespaceDeclaration(ParseName(options.NamespaceName))
                        .AddMembers(classDeclaration))
                .NormalizeWhitespace();
        }

        private CompilationUnitSyntax BuildDeclarationsAst(TranspileContext context, SqExpressSqlTranspilerOptions options)
        {
            var members = context.Descriptors
                .Select(this.BuildDescriptorClass)
                .ToArray();

            return CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("System")),
                    UsingDirective(ParseName("SqExpress")),
                    UsingDirective(ParseName("SqExpress.Syntax.Select")))
                .AddMembers(
                    NamespaceDeclaration(ParseName(options.EffectiveDeclarationsNamespaceName))
                        .AddMembers(members))
                .NormalizeWhitespace();
        }

        private ClassDeclarationSyntax BuildDescriptorClass(TableDescriptor descriptor)
        {
            return descriptor.Kind switch
            {
                DescriptorKind.Table => this.BuildTableDescriptorClass(descriptor),
                DescriptorKind.Cte => this.BuildCteDescriptorClass(descriptor),
                DescriptorKind.SubQuery => this.BuildSubQueryDescriptorClass(descriptor),
                _ => throw new SqExpressSqlTranspilerException($"Unsupported descriptor kind: {descriptor.Kind}")
            };
        }

        private ClassDeclarationSyntax BuildTableDescriptorClass(TableDescriptor descriptor)
        {
            var constructorStatements = descriptor.Columns
                .Select(column =>
                    (RoslynStatementSyntax)ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(column.PropertyName)),
                            this.BuildCreateColumnExpression(column, tableDescriptor: true))))
                .ToArray();

            var baseArguments = new List<ArgumentSyntax>();
            if (descriptor.DatabaseName != null)
            {
                if (descriptor.SchemaName == null)
                {
                    throw new SqExpressSqlTranspilerException("Database-qualified table without schema is not supported.");
                }

                baseArguments.Add(Argument(StringLiteral(descriptor.DatabaseName)));
                baseArguments.Add(Argument(StringLiteral(descriptor.SchemaName)));
                baseArguments.Add(Argument(StringLiteral(descriptor.ObjectName)));
                baseArguments.Add(Argument(IdentifierName("alias")));
            }
            else
            {
                baseArguments.Add(Argument(descriptor.SchemaName != null ? StringLiteral(descriptor.SchemaName) : LiteralExpression(SyntaxKind.NullLiteralExpression)));
                baseArguments.Add(Argument(StringLiteral(descriptor.ObjectName)));
                baseArguments.Add(Argument(IdentifierName("alias")));
            }

            var constructor = ConstructorDeclaration(descriptor.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier("alias"))
                                .WithType(IdentifierName("Alias"))
                                .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))))
                .WithInitializer(
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(SeparatedList(baseArguments))))
                .WithBody(Block(constructorStatements));

            var properties = descriptor.Columns
                .Select(column => this.BuildDescriptorProperty(column, tableDescriptor: true))
                .ToArray();

            return ClassDeclaration(descriptor.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
                .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("TableBase")))))
                .AddMembers(properties)
                .AddMembers(constructor);
        }

        private ClassDeclarationSyntax BuildCteDescriptorClass(TableDescriptor descriptor)
        {
            var constructorStatements = descriptor.Columns
                .Select(column =>
                    (RoslynStatementSyntax)ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(column.PropertyName)),
                            this.BuildCreateColumnExpression(column, tableDescriptor: false))))
                .ToArray();

            var constructor = ConstructorDeclaration(descriptor.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier("alias"))
                                .WithType(IdentifierName("Alias"))
                                .WithDefault(EqualsValueClause(LiteralExpression(SyntaxKind.DefaultLiteralExpression))))))
                .WithInitializer(
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(
                            SeparatedList(new[]
                            {
                                Argument(StringLiteral(descriptor.ObjectName)),
                                Argument(IdentifierName("alias"))
                            }))))
                .WithBody(Block(constructorStatements));

            var createQueryMethod = MethodDeclaration(IdentifierName("IExprSubQuery"), Identifier("CreateQuery"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword))
                .WithExpressionBody(
                    ArrowExpressionClause(
                        ThrowExpression(
                            ObjectCreationExpression(IdentifierName("NotImplementedException"))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(StringLiteral("CTE query transpilation is not implemented yet."))))))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            var properties = descriptor.Columns
                .Select(column => this.BuildDescriptorProperty(column, tableDescriptor: false))
                .ToArray();

            return ClassDeclaration(descriptor.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
                .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("CteBase")))))
                .AddMembers(properties)
                .AddMembers(constructor)
                .AddMembers(createQueryMethod);
        }

        private ClassDeclarationSyntax BuildSubQueryDescriptorClass(TableDescriptor descriptor)
        {
            var constructorStatements = descriptor.Columns
                .Select(column =>
                    (RoslynStatementSyntax)ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(column.PropertyName)),
                            this.BuildCreateColumnExpression(column, tableDescriptor: false))))
                .ToArray();

            var constructor = ConstructorDeclaration(descriptor.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier("alias"))
                                .WithType(IdentifierName("Alias")))))
                .WithInitializer(
                    ConstructorInitializer(
                        SyntaxKind.BaseConstructorInitializer,
                        ArgumentList(SingletonSeparatedList(Argument(IdentifierName("alias"))))))
                .WithBody(Block(constructorStatements));

            var createQueryMethod = MethodDeclaration(IdentifierName("IExprSubQuery"), Identifier("CreateQuery"))
                .AddModifiers(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                .WithExpressionBody(
                    ArrowExpressionClause(
                        ThrowExpression(
                            ObjectCreationExpression(IdentifierName("NotImplementedException"))
                                .WithArgumentList(
                                    ArgumentList(
                                        SingletonSeparatedList(
                                            Argument(StringLiteral("Sub query transpilation is not implemented yet."))))))))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

            var properties = descriptor.Columns
                .Select(column => this.BuildDescriptorProperty(column, tableDescriptor: false))
                .ToArray();

            return ClassDeclaration(descriptor.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
                .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("DerivedTableBase")))))
                .AddMembers(properties)
                .AddMembers(constructor)
                .AddMembers(createQueryMethod);
        }

        private PropertyDeclarationSyntax BuildDescriptorProperty(TableDescriptorColumn column, bool tableDescriptor)
        {
            var typeName = tableDescriptor
                ? column.Kind == DescriptorColumnKind.NVarChar ? "StringTableColumn" : "Int32TableColumn"
                : column.Kind == DescriptorColumnKind.NVarChar ? "StringCustomColumn" : "Int32CustomColumn";

            return PropertyDeclaration(IdentifierName(typeName), Identifier(column.PropertyName))
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithAccessorList(
                    AccessorList(
                        SingletonList(
                            AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)))));
        }

        private ExpressionSyntax BuildCreateColumnExpression(TableDescriptorColumn column, bool tableDescriptor)
        {
            if (column.Kind == DescriptorColumnKind.NVarChar)
            {
                return tableDescriptor
                    ? InvokeMember(ThisExpression(), "CreateStringColumn", StringLiteral(column.SqlName), LiteralExpression(SyntaxKind.NullLiteralExpression), LiteralExpression(SyntaxKind.TrueLiteralExpression))
                    : InvokeMember(ThisExpression(), "CreateStringColumn", StringLiteral(column.SqlName));
            }

            return InvokeMember(ThisExpression(), "CreateInt32Column", StringLiteral(column.SqlName));
        }

        private ExpressionSyntax BuildSelectExpression(QuerySpecification specification, OrderByClause? orderByClause, TranspileContext context)
        {
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
                context.GetOrAddNamedSource(named);
                return;
            }

            if (tableReference is QueryDerivedTable derivedTable)
            {
                context.GetOrAddDerivedSource(derivedTable);
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
                if (!context.TryResolveSource(sourceName, out var source))
                {
                    throw new SqExpressSqlTranspilerException($"Could not resolve SELECT * qualifier '{sourceName}'.");
                }

                return InvokeMember(IdentifierName(source.VariableName), "AllColumns");
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

                var source = context.GetOrAddNamedSource(namedTable);
                return InvokeMember(current, "From", IdentifierName(source.VariableName));
            }

            if (tableReference is QueryDerivedTable derivedTable)
            {
                if (!isRoot)
                {
                    throw new SqExpressSqlTranspilerException("Unexpected standalone table reference in joined table tree.");
                }

                var source = context.GetOrAddDerivedSource(derivedTable);
                return InvokeMember(current, "From", IdentifierName(source.VariableName));
            }

            if (tableReference is JoinParenthesisTableReference joinParenthesis)
            {
                return this.ApplyTableReference(current, joinParenthesis.Join, context, isRoot);
            }

            if (tableReference is QualifiedJoin qualifiedJoin)
            {
                var withLeft = this.ApplyTableReference(current, qualifiedJoin.FirstTableReference, context, isRoot);
                var rightSource = this.ResolveJoinedSource(qualifiedJoin.SecondTableReference, context);
                var onExpression = this.BuildBooleanExpression(qualifiedJoin.SearchCondition, context);

                string joinMethod = qualifiedJoin.QualifiedJoinType switch
                {
                    QualifiedJoinType.Inner => "InnerJoin",
                    QualifiedJoinType.LeftOuter => "LeftJoin",
                    QualifiedJoinType.FullOuter => "FullJoin",
                    QualifiedJoinType.RightOuter => throw new SqExpressSqlTranspilerException("RIGHT JOIN is not supported yet."),
                    _ => throw new SqExpressSqlTranspilerException($"Unsupported qualified join type: {qualifiedJoin.QualifiedJoinType}.")
                };

                return InvokeMember(withLeft, joinMethod, IdentifierName(rightSource.VariableName), onExpression);
            }

            if (tableReference is UnqualifiedJoin unqualifiedJoin)
            {
                var withLeft = this.ApplyTableReference(current, unqualifiedJoin.FirstTableReference, context, isRoot);
                var rightSource = this.ResolveJoinedSource(unqualifiedJoin.SecondTableReference, context);

                return unqualifiedJoin.UnqualifiedJoinType switch
                {
                    UnqualifiedJoinType.CrossJoin => InvokeMember(withLeft, "CrossJoin", IdentifierName(rightSource.VariableName)),
                    _ => throw new SqExpressSqlTranspilerException($"Unsupported unqualified join type: {unqualifiedJoin.UnqualifiedJoinType}.")
                };
            }

            throw new SqExpressSqlTranspilerException($"Unsupported table reference: {tableReference.GetType().Name}.");
        }

        private TableSource ResolveJoinedSource(TableReference tableReference, TranspileContext context)
        {
            if (tableReference is NamedTableReference namedTable)
            {
                return context.GetOrAddNamedSource(namedTable);
            }

            if (tableReference is QueryDerivedTable derivedTable)
            {
                return context.GetOrAddDerivedSource(derivedTable);
            }

            if (tableReference is JoinParenthesisTableReference parenthesized)
            {
                return this.ResolveJoinedSource(parenthesized.Join, context);
            }

            throw new SqExpressSqlTranspilerException("Only named tables and derived subqueries are supported on the right side of JOIN.");
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
                if (IsStringLiteral(comparison.FirstExpression) && comparison.SecondExpression is ColumnReferenceExpression rightStringColumn)
                {
                    context.MarkColumnAsString(rightStringColumn);
                }

                if (IsStringLiteral(comparison.SecondExpression) && comparison.FirstExpression is ColumnReferenceExpression leftStringColumn)
                {
                    context.MarkColumnAsString(leftStringColumn);
                }

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

                if (inPredicate.Values.Any(IsStringLiteral))
                {
                    context.MarkColumnAsString(inColumn);
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

                if (like.FirstExpression is ColumnReferenceExpression likeColumn)
                {
                    context.MarkColumnAsString(likeColumn);
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

                if ((IsStringLiteral(between.SecondExpression) || IsStringLiteral(between.ThirdExpression))
                    && between.FirstExpression is ColumnReferenceExpression betweenColumn)
                {
                    context.MarkColumnAsString(betweenColumn);
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
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new SqExpressSqlTranspilerException("Column name cannot be empty.");
            }

            var registeredColumn = context.RegisterColumnReference(columnReference, DescriptorColumnKind.Int32);
            if (registeredColumn != null)
            {
                var resolvedColumn = registeredColumn.Value;
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    IdentifierName(resolvedColumn.Source.VariableName),
                    IdentifierName(resolvedColumn.Column.PropertyName));
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

        private static bool IsStringLiteral(ScalarExpression expression)
            => expression is StringLiteral;

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

        private static IReadOnlyList<string> ExtractProjectedColumns(QueryExpression queryExpression)
        {
            if (queryExpression is not QuerySpecification specification)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>();
            var exprIndex = 0;

            foreach (var selectElement in specification.SelectElements)
            {
                if (selectElement is not SelectScalarExpression scalar)
                {
                    continue;
                }

                var alias = TryGetSelectAlias(scalar.ColumnName);
                if (string.IsNullOrWhiteSpace(alias) && scalar.Expression is ColumnReferenceExpression colRef)
                {
                    var ids = colRef.MultiPartIdentifier?.Identifiers;
                    alias = ids != null && ids.Count > 0
                        ? ids[ids.Count - 1].Value
                        : null;
                }

                if (string.IsNullOrWhiteSpace(alias))
                {
                    exprIndex++;
                    alias = "Expr" + exprIndex.ToString(CultureInfo.InvariantCulture);
                }

                result.Add(alias!);
            }

            return result;
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

        private static string NormalizeTypeIdentifier(string name, string fallback)
        {
            var normalized = NormalizeIdentifier(name, fallback);
            if (string.IsNullOrEmpty(normalized))
            {
                return fallback;
            }

            return char.ToUpperInvariant(normalized[0]) + normalized.Substring(1);
        }

        private sealed class TranspileContext
        {
            private readonly List<TableDescriptor> _descriptors = new();
            private readonly List<TableSource> _sources = new();
            private readonly List<LocalDeclarationStatementSyntax> _sourceDeclarations = new();
            private readonly Dictionary<NamedTableReference, TableSource> _namedSourceMap = new(ReferenceEqualityComparer<NamedTableReference>.Instance);
            private readonly Dictionary<QueryDerivedTable, TableSource> _derivedSourceMap = new(ReferenceEqualityComparer<QueryDerivedTable>.Instance);
            private readonly Dictionary<string, TableSource> _sourceByAlias = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, TableSource?> _sourceByObjectName = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, TableDescriptor> _cteDescriptors = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _variableNames = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _classNames = new(StringComparer.OrdinalIgnoreCase);

            public TranspileContext(SqExpressSqlTranspilerOptions options)
            {
            }

            public IReadOnlyList<TableDescriptor> Descriptors => this._descriptors;

            public IReadOnlyList<LocalDeclarationStatementSyntax> SourceDeclarations => this._sourceDeclarations;

            public void RegisterCtes(WithCtesAndXmlNamespaces? withClause)
            {
                if (withClause == null)
                {
                    return;
                }

                foreach (var cte in withClause.CommonTableExpressions)
                {
                    var cteName = cte.ExpressionName?.Value;
                    if (string.IsNullOrWhiteSpace(cteName))
                    {
                        continue;
                    }

                    if (this._cteDescriptors.ContainsKey(cteName!))
                    {
                        continue;
                    }

                    var descriptor = new TableDescriptor(
                        className: this.CreateClassName(cteName + "Cte", "GeneratedCte"),
                        kind: DescriptorKind.Cte,
                        objectName: cteName!,
                        schemaName: null,
                        databaseName: null);

                    var columns = cte.Columns.Count > 0
                        ? cte.Columns.Select(i => i.Value).Where(i => !string.IsNullOrWhiteSpace(i)).ToList()
                        : ExtractProjectedColumns(cte.QueryExpression).ToList();

                    foreach (var column in columns)
                    {
                        descriptor.GetOrAddColumn(column, DescriptorColumnKind.Int32);
                    }

                    this._cteDescriptors[cteName!] = descriptor;
                    this._descriptors.Add(descriptor);
                }
            }

            public TableSource GetOrAddNamedSource(NamedTableReference tableReference)
            {
                if (this._namedSourceMap.TryGetValue(tableReference, out var existing))
                {
                    return existing;
                }

                if (tableReference.SchemaObject == null)
                {
                    throw new SqExpressSqlTranspilerException("Only schema object table references are supported.");
                }

                if (tableReference.SchemaObject.ServerIdentifier != null)
                {
                    throw new SqExpressSqlTranspilerException("Server-qualified table names are not supported.");
                }

                var objectName = tableReference.SchemaObject.BaseIdentifier?.Value;
                if (string.IsNullOrWhiteSpace(objectName))
                {
                    throw new SqExpressSqlTranspilerException("Table name is missing.");
                }

                var alias = tableReference.Alias?.Value;
                TableDescriptor descriptor;

                if (tableReference.SchemaObject.DatabaseIdentifier == null
                    && tableReference.SchemaObject.SchemaIdentifier == null
                    && this._cteDescriptors.TryGetValue(objectName!, out var cteDescriptor))
                {
                    descriptor = cteDescriptor;
                }
                else
                {
                    descriptor = new TableDescriptor(
                        className: this.CreateClassName((alias ?? objectName) + "Table", "GeneratedTable"),
                        kind: DescriptorKind.Table,
                        objectName: objectName!,
                        schemaName: tableReference.SchemaObject.SchemaIdentifier?.Value,
                        databaseName: tableReference.SchemaObject.DatabaseIdentifier?.Value);

                    this._descriptors.Add(descriptor);
                }

                var source = this.RegisterSource(descriptor, alias ?? objectName!, alias, objectName!);
                this._namedSourceMap[tableReference] = source;
                return source;
            }

            public TableSource GetOrAddDerivedSource(QueryDerivedTable queryDerivedTable)
            {
                if (this._derivedSourceMap.TryGetValue(queryDerivedTable, out var existing))
                {
                    return existing;
                }

                var alias = queryDerivedTable.Alias?.Value;
                if (string.IsNullOrWhiteSpace(alias))
                {
                    throw new SqExpressSqlTranspilerException("Derived table alias cannot be empty.");
                }

                var descriptor = new TableDescriptor(
                    className: this.CreateClassName(alias + "SubQuery", "GeneratedSubQuery"),
                    kind: DescriptorKind.SubQuery,
                    objectName: alias!,
                    schemaName: null,
                    databaseName: null);

                foreach (var column in ExtractProjectedColumns(queryDerivedTable.QueryExpression))
                {
                    descriptor.GetOrAddColumn(column, DescriptorColumnKind.Int32);
                }

                this._descriptors.Add(descriptor);

                var source = this.RegisterSource(descriptor, alias!, alias, alias!);
                this._derivedSourceMap[queryDerivedTable] = source;
                return source;
            }

            public bool TryResolveSource(string sourceName, out TableSource source)
            {
                if (this._sourceByAlias.TryGetValue(sourceName, out var aliasSource) && aliasSource != null)
                {
                    source = aliasSource;
                    return true;
                }

                if (this._sourceByObjectName.TryGetValue(sourceName, out var byName) && byName != null)
                {
                    source = byName;
                    return true;
                }

                source = null!;
                return false;
            }

            public void MarkColumnAsString(ColumnReferenceExpression columnReference)
            {
                this.RegisterColumnReference(columnReference, DescriptorColumnKind.NVarChar);
            }

            public RegisteredDescriptorColumn? RegisterColumnReference(ColumnReferenceExpression columnReference, DescriptorColumnKind typeHint)
            {
                if (columnReference.ColumnType == ColumnType.Wildcard)
                {
                    return null;
                }

                var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
                if (identifiers == null || identifiers.Count < 1)
                {
                    return null;
                }

                var columnName = identifiers[identifiers.Count - 1].Value;
                if (string.IsNullOrWhiteSpace(columnName))
                {
                    return null;
                }

                TableSource? source = null;
                if (identifiers.Count > 1)
                {
                    for (int i = identifiers.Count - 2; i >= 0; i--)
                    {
                        var sourceName = identifiers[i].Value;
                        if (!string.IsNullOrWhiteSpace(sourceName) && this.TryResolveSource(sourceName, out var resolved))
                        {
                            source = resolved;
                            break;
                        }
                    }
                }
                else if (this._sources.Count == 1)
                {
                    source = this._sources[0];
                }

                if (source == null)
                {
                    return null;
                }

                var descriptorColumn = source.Descriptor.GetOrAddColumn(columnName!, typeHint);
                return new RegisteredDescriptorColumn(source, descriptorColumn);
            }

            private TableSource RegisterSource(TableDescriptor descriptor, string variableSeed, string? alias, string objectName)
            {
                var source = new TableSource(descriptor, this.CreateVariableName(variableSeed), alias);
                this._sources.Add(source);
                this._sourceDeclarations.Add(this.CreateSourceDeclaration(source));

                if (!string.IsNullOrWhiteSpace(alias))
                {
                    this._sourceByAlias[alias!] = source;
                }

                if (this._sourceByObjectName.TryGetValue(objectName, out var existingByName))
                {
                    if (!ReferenceEquals(existingByName, source))
                    {
                        this._sourceByObjectName[objectName] = null;
                    }
                }
                else
                {
                    this._sourceByObjectName[objectName] = source;
                }

                return source;
            }

            private LocalDeclarationStatementSyntax CreateSourceDeclaration(TableSource source)
            {
                var arguments = new List<ArgumentSyntax>();

                if (!string.IsNullOrWhiteSpace(source.SqlAlias))
                {
                    arguments.Add(Argument(StringLiteral(source.SqlAlias!)));
                }

                if (source.Descriptor.Kind == DescriptorKind.SubQuery && arguments.Count == 0)
                {
                    throw new SqExpressSqlTranspilerException("Derived table alias cannot be empty.");
                }

                return LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator(Identifier(source.VariableName))
                                .WithInitializer(
                                    EqualsValueClause(
                                        ObjectCreationExpression(IdentifierName(source.Descriptor.ClassName))
                                            .WithArgumentList(ArgumentList(SeparatedList(arguments)))))));
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

            private string CreateClassName(string source, string fallback)
            {
                var normalized = NormalizeTypeIdentifier(source, fallback);

                var candidate = normalized;
                var index = 0;
                while (this._classNames.Contains(candidate))
                {
                    index++;
                    candidate = normalized + index.ToString(CultureInfo.InvariantCulture);
                }

                this._classNames.Add(candidate);
                return candidate;
            }
        }

        private readonly struct RegisteredDescriptorColumn
        {
            public RegisteredDescriptorColumn(TableSource source, TableDescriptorColumn column)
            {
                this.Source = source;
                this.Column = column;
            }

            public TableSource Source { get; }

            public TableDescriptorColumn Column { get; }
        }

        private sealed class TableSource
        {
            public TableSource(TableDescriptor descriptor, string variableName, string? sqlAlias)
            {
                this.Descriptor = descriptor;
                this.VariableName = variableName;
                this.SqlAlias = sqlAlias;
            }

            public TableDescriptor Descriptor { get; }

            public string VariableName { get; }

            public string? SqlAlias { get; }
        }

        private sealed class TableDescriptor
        {
            private readonly List<TableDescriptorColumn> _columns = new();
            private readonly Dictionary<string, TableDescriptorColumn> _columnMap = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _propertyNames = new(StringComparer.OrdinalIgnoreCase);

            public TableDescriptor(string className, DescriptorKind kind, string objectName, string? schemaName, string? databaseName)
            {
                this.ClassName = className;
                this.Kind = kind;
                this.ObjectName = objectName;
                this.SchemaName = schemaName;
                this.DatabaseName = databaseName;
            }

            public string ClassName { get; }

            public DescriptorKind Kind { get; }

            public string ObjectName { get; }

            public string? SchemaName { get; }

            public string? DatabaseName { get; }

            public IReadOnlyList<TableDescriptorColumn> Columns => this._columns;

            public TableDescriptorColumn GetOrAddColumn(string sqlName, DescriptorColumnKind kind)
            {
                if (this._columnMap.TryGetValue(sqlName, out var existing))
                {
                    if (kind == DescriptorColumnKind.NVarChar && existing.Kind != DescriptorColumnKind.NVarChar)
                    {
                        existing.Kind = DescriptorColumnKind.NVarChar;
                    }

                    return existing;
                }

                var normalizedProperty = NormalizeTypeIdentifier(sqlName, "Column");
                var propertyName = normalizedProperty;
                var index = 0;
                while (this._propertyNames.Contains(propertyName))
                {
                    index++;
                    propertyName = normalizedProperty + index.ToString(CultureInfo.InvariantCulture);
                }

                this._propertyNames.Add(propertyName);

                var column = new TableDescriptorColumn(sqlName, propertyName, kind);
                this._columnMap[sqlName] = column;
                this._columns.Add(column);
                return column;
            }
        }

        private sealed class TableDescriptorColumn
        {
            public TableDescriptorColumn(string sqlName, string propertyName, DescriptorColumnKind kind)
            {
                this.SqlName = sqlName;
                this.PropertyName = propertyName;
                this.Kind = kind;
            }

            public string SqlName { get; }

            public string PropertyName { get; }

            public DescriptorColumnKind Kind { get; set; }
        }

        private enum DescriptorKind
        {
            Table,
            Cte,
            SubQuery
        }

        private enum DescriptorColumnKind
        {
            Int32,
            NVarChar
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
