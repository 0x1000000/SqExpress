using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.SqlServer.TransactSql.ScriptDom;
using SqExpress.Syntax.Functions;
using SqExpress.Syntax.Functions.Known;
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
            if (selectStatement.Into != null)
            {
                throw new SqExpressSqlTranspilerException("SELECT INTO is not supported yet.");
            }

            var context = new TranspileContext(options);
            context.RegisterCtes(selectStatement.WithCtesAndXmlNamespaces);
            this.PreRegisterQueryExpression(selectStatement.QueryExpression, context);
            var queryExpression = this.BuildQueryExpression(selectStatement.QueryExpression, context);
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
                    UsingDirective(ParseName("SqExpress.Syntax.Functions")),
                    UsingDirective(ParseName("SqExpress.Syntax.Functions.Known")),
                    UsingDirective(ParseName(options.EffectiveDeclarationsNamespaceName)),
                    UsingDirective(ParseName("SqExpress.SqQueryBuilder")).WithStaticKeyword(Token(SyntaxKind.StaticKeyword)))
                .AddMembers(
                    NamespaceDeclaration(ParseName(options.NamespaceName))
                        .AddMembers(classDeclaration))
                .NormalizeWhitespace();
        }

        private CompilationUnitSyntax BuildDeclarationsAst(TranspileContext context, SqExpressSqlTranspilerOptions options)
        {
            var members = new List<MemberDeclarationSyntax>();
            for (var i = 0; i < context.Descriptors.Count; i++)
            {
                members.Add(this.BuildDescriptorClass(context.Descriptors[i], context));
            }

            return CompilationUnit()
                .AddUsings(
                    UsingDirective(ParseName("System")),
                    UsingDirective(ParseName("SqExpress")),
                    UsingDirective(ParseName("SqExpress.Syntax.Select")),
                    UsingDirective(ParseName("SqExpress.Syntax.Functions")),
                    UsingDirective(ParseName("SqExpress.Syntax.Functions.Known")),
                    UsingDirective(ParseName("SqExpress.SqQueryBuilder")).WithStaticKeyword(Token(SyntaxKind.StaticKeyword)))
                .AddMembers(
                    NamespaceDeclaration(ParseName(options.EffectiveDeclarationsNamespaceName))
                        .AddMembers(members.ToArray()))
                .NormalizeWhitespace();
        }

        private ClassDeclarationSyntax BuildDescriptorClass(TableDescriptor descriptor, TranspileContext context)
        {
            return descriptor.Kind switch
            {
                DescriptorKind.Table => this.BuildTableDescriptorClass(descriptor),
                DescriptorKind.Cte => this.BuildCteDescriptorClass(descriptor, context),
                DescriptorKind.SubQuery => this.BuildSubQueryDescriptorClass(descriptor, context),
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

        private ClassDeclarationSyntax BuildCteDescriptorClass(TableDescriptor descriptor, TranspileContext parentContext)
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

            var cteQueryContext = parentContext.CreateChild();
            if (descriptor.QueryExpression == null)
            {
                throw new SqExpressSqlTranspilerException("CTE query expression is required.");
            }

            this.PreRegisterQueryExpression(descriptor.QueryExpression, cteQueryContext);
            var cteQueryExpression = this.BuildQueryExpression(descriptor.QueryExpression, cteQueryContext);
            var cteQueryDoneExpression = InvokeMember(cteQueryExpression, "Done");
            var createQueryBody = new List<RoslynStatementSyntax>();
            createQueryBody.AddRange(cteQueryContext.SourceDeclarations);
            createQueryBody.Add(ReturnStatement(cteQueryDoneExpression));

            var createQueryMethod = MethodDeclaration(IdentifierName("IExprSubQuery"), Identifier("CreateQuery"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.OverrideKeyword))
                .WithBody(Block(createQueryBody));

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

        private ClassDeclarationSyntax BuildSubQueryDescriptorClass(TableDescriptor descriptor, TranspileContext parentContext)
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

            var subQueryContext = parentContext.CreateChild();
            if (descriptor.QueryExpression == null)
            {
                throw new SqExpressSqlTranspilerException("Derived subquery expression is required.");
            }

            this.PreRegisterQueryExpression(descriptor.QueryExpression, subQueryContext);
            var subQueryExpression = this.BuildQueryExpression(descriptor.QueryExpression, subQueryContext);
            var subQueryDoneExpression = InvokeMember(subQueryExpression, "Done");
            var createQueryBody = new List<RoslynStatementSyntax>();
            createQueryBody.AddRange(subQueryContext.SourceDeclarations);
            createQueryBody.Add(ReturnStatement(subQueryDoneExpression));

            var createQueryMethod = MethodDeclaration(IdentifierName("IExprSubQuery"), Identifier("CreateQuery"))
                .AddModifiers(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                .WithBody(Block(createQueryBody));

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

        private void PreRegisterQueryExpression(QueryExpression queryExpression, TranspileContext context)
        {
            if (queryExpression is QueryParenthesisExpression parenthesized)
            {
                this.PreRegisterQueryExpression(parenthesized.QueryExpression, context);
                return;
            }

            if (queryExpression is QuerySpecification specification)
            {
                if (specification.FromClause != null)
                {
                    if (specification.FromClause.TableReferences.Count != 1)
                    {
                        throw new SqExpressSqlTranspilerException("Only one root FROM table-reference is supported.");
                    }

                    this.PreRegisterTableReferences(specification.FromClause.TableReferences[0], context);
                }

                return;
            }

            if (queryExpression is BinaryQueryExpression binaryQuery)
            {
                this.PreRegisterQueryExpression(binaryQuery.FirstQueryExpression, context);
                this.PreRegisterQueryExpression(binaryQuery.SecondQueryExpression, context);
                return;
            }

            throw new SqExpressSqlTranspilerException($"Unsupported query expression: {queryExpression.GetType().Name}.");
        }

        private ExpressionSyntax BuildQueryExpression(QueryExpression queryExpression, TranspileContext context, bool allowOrderByAndOffset = true)
        {
            if (queryExpression is QueryParenthesisExpression parenthesized)
            {
                return this.BuildQueryExpression(parenthesized.QueryExpression, context, allowOrderByAndOffset);
            }

            if (queryExpression is QuerySpecification specification)
            {
                if (!allowOrderByAndOffset
                    && (specification.OrderByClause != null || specification.OffsetClause != null))
                {
                    throw new SqExpressSqlTranspilerException("ORDER BY/OFFSET is not supported inside set-operation operands.");
                }

                if (specification.HavingClause != null)
                {
                    throw new SqExpressSqlTranspilerException("HAVING is not supported yet.");
                }

                return this.BuildSelectExpression(specification, specification.OrderByClause, context);
            }

            if (queryExpression is BinaryQueryExpression binaryQuery)
            {
                if (binaryQuery.ForClause != null)
                {
                    throw new SqExpressSqlTranspilerException("FOR clause is not supported yet.");
                }

                var left = this.BuildQueryExpression(binaryQuery.FirstQueryExpression, context, allowOrderByAndOffset: false);
                var right = this.BuildQueryExpression(binaryQuery.SecondQueryExpression, context, allowOrderByAndOffset: false);

                var setMethod = binaryQuery.BinaryQueryExpressionType switch
                {
                    BinaryQueryExpressionType.Union => binaryQuery.All ? "UnionAll" : "Union",
                    BinaryQueryExpressionType.Except => binaryQuery.All
                        ? throw new SqExpressSqlTranspilerException("EXCEPT ALL is not supported.")
                        : "Except",
                    BinaryQueryExpressionType.Intersect => binaryQuery.All
                        ? throw new SqExpressSqlTranspilerException("INTERSECT ALL is not supported.")
                        : "Intersect",
                    _ => throw new SqExpressSqlTranspilerException($"Unsupported query binary operation: {binaryQuery.BinaryQueryExpressionType}.")
                };

                var current = InvokeMember(left, setMethod, right);

                if (binaryQuery.OrderByClause != null)
                {
                    var orderByArguments = binaryQuery.OrderByClause
                        .OrderByElements
                        .Select(item => this.BuildOrderBy(item, context))
                        .ToList();

                    if (orderByArguments.Count > 0)
                    {
                        current = InvokeMember(current, "OrderBy", orderByArguments);
                    }
                }

                if (binaryQuery.OffsetClause != null)
                {
                    if (binaryQuery.OrderByClause == null)
                    {
                        throw new SqExpressSqlTranspilerException("OFFSET/FETCH requires ORDER BY.");
                    }

                    current = this.ApplyOffsetFetch(current, binaryQuery.OffsetClause);
                }

                return current;
            }

            throw new SqExpressSqlTranspilerException($"Unsupported query expression: {queryExpression.GetType().Name}.");
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
                var topExpression = UnwrapParentheses(this.BuildScalarExpression(top.Expression, context, wrapLiterals: false));
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

            if (specification.GroupByClause != null)
            {
                var groupByColumns = this.BuildGroupByColumns(specification.GroupByClause, context);
                if (groupByColumns.Count == 0)
                {
                    throw new SqExpressSqlTranspilerException("GROUP BY cannot be empty.");
                }

                current = InvokeMember(current, "GroupBy", Prepend(groupByColumns[0], groupByColumns.Skip(1).ToList()));
            }

            if (specification.HavingClause != null)
            {
                throw new SqExpressSqlTranspilerException("HAVING is not supported yet.");
            }

            if (specification.ForClause != null)
            {
                throw new SqExpressSqlTranspilerException("FOR clause is not supported yet.");
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

            if (specification.OffsetClause != null)
            {
                if (orderByClause == null)
                {
                    throw new SqExpressSqlTranspilerException("OFFSET/FETCH requires ORDER BY.");
                }

                current = this.ApplyOffsetFetch(current, specification.OffsetClause);
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

            if (tableReference is SchemaObjectFunctionTableReference schemaFunction)
            {
                context.GetOrAddSchemaFunctionSource(schemaFunction, this);
                return;
            }

            if (tableReference is BuiltInFunctionTableReference builtInFunction)
            {
                context.GetOrAddBuiltInFunctionSource(builtInFunction, this);
                return;
            }

            if (tableReference is GlobalFunctionTableReference globalFunction)
            {
                context.GetOrAddGlobalFunctionSource(globalFunction, this);
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
                var expression = this.BuildScalarExpression(
                    scalar.Expression,
                    context,
                    wrapLiterals: IsLiteralOnlyExpression(scalar.Expression));
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

            if (tableReference is SchemaObjectFunctionTableReference schemaFunctionTable)
            {
                if (!isRoot)
                {
                    throw new SqExpressSqlTranspilerException("Unexpected standalone table reference in joined table tree.");
                }

                var source = context.GetOrAddSchemaFunctionSource(schemaFunctionTable, this);
                return InvokeMember(current, "From", IdentifierName(source.VariableName));
            }

            if (tableReference is BuiltInFunctionTableReference builtInFunctionTable)
            {
                if (!isRoot)
                {
                    throw new SqExpressSqlTranspilerException("Unexpected standalone table reference in joined table tree.");
                }

                var source = context.GetOrAddBuiltInFunctionSource(builtInFunctionTable, this);
                return InvokeMember(current, "From", IdentifierName(source.VariableName));
            }

            if (tableReference is GlobalFunctionTableReference globalFunctionTable)
            {
                if (!isRoot)
                {
                    throw new SqExpressSqlTranspilerException("Unexpected standalone table reference in joined table tree.");
                }

                var source = context.GetOrAddGlobalFunctionSource(globalFunctionTable, this);
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
                    UnqualifiedJoinType.CrossApply => InvokeMember(withLeft, "CrossApply", IdentifierName(rightSource.VariableName)),
                    UnqualifiedJoinType.OuterApply => InvokeMember(withLeft, "OuterApply", IdentifierName(rightSource.VariableName)),
                    _ => throw new SqExpressSqlTranspilerException($"Unsupported unqualified join type: {unqualifiedJoin.UnqualifiedJoinType}.")
                };
            }

            throw new SqExpressSqlTranspilerException($"Unsupported table reference: {tableReference.GetType().Name}.");
        }

        private ExpressionSyntax ApplyOffsetFetch(ExpressionSyntax current, OffsetClause offsetClause)
        {
            if (!TryExtractInt32Constant(offsetClause.OffsetExpression, out var offset))
            {
                throw new SqExpressSqlTranspilerException("OFFSET/FETCH currently supports only integer constants.");
            }

            if (offset < 0)
            {
                throw new SqExpressSqlTranspilerException("OFFSET cannot be negative.");
            }

            if (offsetClause.FetchExpression == null)
            {
                return InvokeMember(
                    current,
                    "Offset",
                    LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(offset)));
            }

            if (!TryExtractInt32Constant(offsetClause.FetchExpression, out var fetch))
            {
                throw new SqExpressSqlTranspilerException("OFFSET/FETCH currently supports only integer constants.");
            }

            if (fetch < 0)
            {
                throw new SqExpressSqlTranspilerException("FETCH cannot be negative.");
            }

            return InvokeMember(
                current,
                "OffsetFetch",
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(offset)),
                LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(fetch)));
        }

        private ExpressionSyntax BuildTableFunctionSourceExpression(SchemaObjectFunctionTableReference functionReference, TranspileContext context)
        {
            if (functionReference.ForPath)
            {
                throw new SqExpressSqlTranspilerException("FOR PATH table functions are not supported.");
            }

            var schemaObject = functionReference.SchemaObject
                ?? throw new SqExpressSqlTranspilerException("Table function name is missing.");

            if (schemaObject.ServerIdentifier != null)
            {
                throw new SqExpressSqlTranspilerException("Server-qualified table functions are not supported.");
            }

            var functionName = schemaObject.BaseIdentifier?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                throw new SqExpressSqlTranspilerException("Table function name cannot be empty.");
            }

            var args = functionReference.Parameters
                .Select(p => this.BuildScalarExpression(p, context, wrapLiterals: false))
                .ToList();

            ExpressionSyntax functionExpression;
            if (schemaObject.DatabaseIdentifier != null)
            {
                var schemaName = schemaObject.SchemaIdentifier?.Value;
                if (string.IsNullOrWhiteSpace(schemaName))
                {
                    throw new SqExpressSqlTranspilerException("Database-qualified table function without schema is not supported.");
                }

                functionExpression = args.Count == 0
                    ? Invoke(
                        "TableFunctionDbCustom",
                        StringLiteral(schemaObject.DatabaseIdentifier.Value),
                        StringLiteral(schemaName!),
                        StringLiteral(functionName!))
                    : Invoke(
                        "TableFunctionDbCustom",
                        Prepend(
                            StringLiteral(schemaObject.DatabaseIdentifier.Value),
                            Prepend(
                                StringLiteral(schemaName!),
                                Prepend(StringLiteral(functionName!), args))));
            }
            else if (schemaObject.SchemaIdentifier != null)
            {
                functionExpression = args.Count == 0
                    ? Invoke(
                        "TableFunctionCustom",
                        StringLiteral(schemaObject.SchemaIdentifier.Value),
                        StringLiteral(functionName!))
                    : Invoke(
                        "TableFunctionCustom",
                        Prepend(
                            StringLiteral(schemaObject.SchemaIdentifier.Value),
                            Prepend(StringLiteral(functionName!), args)));
            }
            else
            {
                functionExpression = args.Count == 0
                    ? Invoke("TableFunctionSys", StringLiteral(functionName!))
                    : Invoke("TableFunctionSys", Prepend(StringLiteral(functionName!), args));
            }

            if (!string.IsNullOrWhiteSpace(functionReference.Alias?.Value))
            {
                functionExpression = InvokeMember(
                    functionExpression,
                    "As",
                    Invoke("TableAlias", StringLiteral(functionReference.Alias!.Value)));
            }

            return functionExpression;
        }

        private ExpressionSyntax BuildTableFunctionSourceExpression(BuiltInFunctionTableReference functionReference, TranspileContext context)
        {
            if (functionReference.ForPath)
            {
                throw new SqExpressSqlTranspilerException("FOR PATH table functions are not supported.");
            }

            var functionName = functionReference.Name?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                throw new SqExpressSqlTranspilerException("Table function name cannot be empty.");
            }

            var args = functionReference.Parameters
                .Select(p => this.BuildScalarExpression(p, context, wrapLiterals: false))
                .ToList();

            var functionExpression = args.Count == 0
                ? Invoke("TableFunctionSys", StringLiteral(functionName!))
                : Invoke("TableFunctionSys", Prepend(StringLiteral(functionName!), args));

            if (!string.IsNullOrWhiteSpace(functionReference.Alias?.Value))
            {
                functionExpression = InvokeMember(
                    functionExpression,
                    "As",
                    Invoke("TableAlias", StringLiteral(functionReference.Alias!.Value)));
            }

            return functionExpression;
        }

        private ExpressionSyntax BuildTableFunctionSourceExpression(GlobalFunctionTableReference functionReference, TranspileContext context)
        {
            if (functionReference.ForPath)
            {
                throw new SqExpressSqlTranspilerException("FOR PATH table functions are not supported.");
            }

            var functionName = functionReference.Name?.Value;
            if (string.IsNullOrWhiteSpace(functionName))
            {
                throw new SqExpressSqlTranspilerException("Table function name cannot be empty.");
            }

            var args = functionReference.Parameters
                .Select(p => this.BuildScalarExpression(p, context, wrapLiterals: false))
                .ToList();

            var functionExpression = args.Count == 0
                ? Invoke("TableFunctionSys", StringLiteral(functionName!))
                : Invoke("TableFunctionSys", Prepend(StringLiteral(functionName!), args));

            if (!string.IsNullOrWhiteSpace(functionReference.Alias?.Value))
            {
                functionExpression = InvokeMember(
                    functionExpression,
                    "As",
                    Invoke("TableAlias", StringLiteral(functionReference.Alias!.Value)));
            }

            return functionExpression;
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

            if (tableReference is SchemaObjectFunctionTableReference schemaFunctionTable)
            {
                return context.GetOrAddSchemaFunctionSource(schemaFunctionTable, this);
            }

            if (tableReference is BuiltInFunctionTableReference builtInFunctionTable)
            {
                return context.GetOrAddBuiltInFunctionSource(builtInFunctionTable, this);
            }

            if (tableReference is GlobalFunctionTableReference globalFunctionTable)
            {
                return context.GetOrAddGlobalFunctionSource(globalFunctionTable, this);
            }

            if (tableReference is JoinParenthesisTableReference parenthesized)
            {
                return this.ResolveJoinedSource(parenthesized.Join, context);
            }

            throw new SqExpressSqlTranspilerException("Only named tables and derived subqueries are supported on the right side of JOIN.");
        }

        private IReadOnlyList<ExpressionSyntax> BuildGroupByColumns(GroupByClause groupByClause, TranspileContext context)
        {
            if (groupByClause.All)
            {
                throw new SqExpressSqlTranspilerException("GROUP BY ALL is not supported.");
            }

            if (groupByClause.GroupByOption != GroupByOption.None)
            {
                throw new SqExpressSqlTranspilerException($"GROUP BY option '{groupByClause.GroupByOption}' is not supported.");
            }

            if (groupByClause.GroupingSpecifications.Count == 0)
            {
                throw new SqExpressSqlTranspilerException("GROUP BY cannot be empty.");
            }

            var columns = new List<ExpressionSyntax>();
            foreach (var groupingSpecification in groupByClause.GroupingSpecifications)
            {
                if (groupingSpecification is not ExpressionGroupingSpecification expressionGrouping)
                {
                    throw new SqExpressSqlTranspilerException($"Unsupported GROUP BY item: {groupingSpecification.GetType().Name}.");
                }

                if (expressionGrouping.Expression is not ColumnReferenceExpression columnReference)
                {
                    throw new SqExpressSqlTranspilerException("GROUP BY currently supports only column references.");
                }

                columns.Add(this.BuildColumnExpression(columnReference, context));
            }

            return columns;
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
                    ParenthesizeIfNeeded(this.BuildScalarExpression(comparison.FirstExpression, context, wrapLiterals: false)),
                    ParenthesizeIfNeeded(this.BuildScalarExpression(comparison.SecondExpression, context, wrapLiterals: false)));
            }

            if (expression is BooleanIsNullExpression isNull)
            {
                var test = this.BuildScalarExpression(isNull.Expression, context, wrapLiterals: false);
                return Invoke(isNull.IsNot ? "IsNotNull" : "IsNull", test);
            }

            if (expression is InPredicate inPredicate)
            {
                if (inPredicate.Expression is not ColumnReferenceExpression inColumn)
                {
                    throw new SqExpressSqlTranspilerException("IN predicate is supported only for column references.");
                }

                var column = this.BuildColumnExpression(inColumn, context);

                ExpressionSyntax inCall;
                if (inPredicate.Subquery != null)
                {
                    var inSubQuery = this.BuildSubQueryExpression(inPredicate.Subquery, context);
                    inCall = InvokeMember(column, "In", inSubQuery);
                }
                else
                {
                    if (inPredicate.Values.Count < 1)
                    {
                        throw new SqExpressSqlTranspilerException("IN predicate cannot be empty.");
                    }

                    if (inPredicate.Values.Any(IsStringLiteral))
                    {
                        context.MarkColumnAsString(inColumn);
                    }

                    var values = inPredicate.Values.Select(item => this.BuildScalarExpression(item, context, wrapLiterals: false)).ToList();
                    inCall = InvokeMember(column, "In", values);
                }

                if (inPredicate.NotDefined)
                {
                    return PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, ParenthesizeIfNeeded(inCall));
                }

                return inCall;
            }

            if (expression is ExistsPredicate existsPredicate)
            {
                var existsSubQuery = this.BuildSubQueryExpression(existsPredicate.Subquery, context);
                return Invoke("Exists", existsSubQuery);
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

                var test = this.BuildScalarExpression(like.FirstExpression, context, wrapLiterals: false);
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

                var test = this.BuildScalarExpression(between.FirstExpression, context, wrapLiterals: false);
                var start = this.BuildScalarExpression(between.SecondExpression, context, wrapLiterals: false);
                var end = this.BuildScalarExpression(between.ThirdExpression, context, wrapLiterals: false);

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

        private ExpressionSyntax BuildScalarExpression(ScalarExpression expression, TranspileContext context, bool wrapLiterals)
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
                return wrapLiterals
                    ? Invoke("Literal", StringLiteral(stringLiteral.Value))
                    : StringLiteral(stringLiteral.Value);
            }

            if (expression is IntegerLiteral integerLiteral)
            {
                return wrapLiterals
                    ? Invoke("Literal", NumericLiteral(integerLiteral.Value))
                    : NumericLiteral(integerLiteral.Value);
            }

            if (expression is NumericLiteral numericLiteral)
            {
                return wrapLiterals
                    ? Invoke("Literal", DecimalOrDoubleLiteral(numericLiteral.Value))
                    : DecimalOrDoubleLiteral(numericLiteral.Value);
            }

            if (expression is MoneyLiteral moneyLiteral)
            {
                return wrapLiterals
                    ? Invoke("Literal", DecimalOrDoubleLiteral(moneyLiteral.Value))
                    : DecimalOrDoubleLiteral(moneyLiteral.Value);
            }

            if (expression is CoalesceExpression coalesceExpression)
            {
                if (coalesceExpression.Expressions.Count < 2)
                {
                    throw new SqExpressSqlTranspilerException("COALESCE expression must have at least two arguments.");
                }

                var args = coalesceExpression.Expressions
                    .Select(item => this.BuildScalarExpression(item, context, wrapLiterals))
                    .ToList();
                return Invoke("Coalesce", args);
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

                return PrefixUnaryExpression(unaryKind, ParenthesizeIfNeeded(this.BuildScalarExpression(unary.Expression, context, wrapLiterals)));
            }

            if (expression is ParenthesisExpression parenthesis)
            {
                return ParenthesizedExpression(this.BuildScalarExpression(parenthesis.Expression, context, wrapLiterals));
            }

            if (expression is BinaryExpression binary)
            {
                var binaryKind = MapBinaryKind(binary.BinaryExpressionType);
                return BinaryExpression(
                    binaryKind,
                    ParenthesizeIfNeeded(this.BuildScalarExpression(binary.FirstExpression, context, wrapLiterals)),
                    ParenthesizeIfNeeded(this.BuildScalarExpression(binary.SecondExpression, context, wrapLiterals)));
            }

            if (expression is CaseExpression caseExpression)
            {
                return this.BuildCaseExpression(caseExpression, context, wrapLiterals: false);
            }

            if (expression is FunctionCall functionCall)
            {
                return this.BuildFunctionCall(functionCall, context, wrapLiterals);
            }

            if (expression is ScalarSubquery scalarSubquery)
            {
                var subQuery = this.BuildSubQueryExpression(scalarSubquery, context);
                return Invoke("ValueQuery", subQuery);
            }

            if (expression is CastCall castCall)
            {
                return Invoke("Cast", this.BuildScalarExpression(castCall.Parameter, context, wrapLiterals), this.BuildSqlType(castCall.DataType));
            }

            throw new SqExpressSqlTranspilerException($"Unsupported scalar expression: {expression.GetType().Name}.");
        }

        private ExpressionSyntax BuildSubQueryExpression(ScalarSubquery scalarSubquery, TranspileContext context)
        {
            if (scalarSubquery.Collation != null)
            {
                throw new SqExpressSqlTranspilerException("Subquery collation is not supported.");
            }

            var subQueryContext = context.CreateChild(shareVariableNames: true, inheritSourceResolution: true);
            this.PreRegisterQueryExpression(scalarSubquery.QueryExpression, subQueryContext);
            var subQueryExpression = this.BuildQueryExpression(scalarSubquery.QueryExpression, subQueryContext);
            context.AbsorbSourceDeclarations(subQueryContext);

            return subQueryExpression;
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

        private ExpressionSyntax BuildCaseExpression(CaseExpression caseExpression, TranspileContext context, bool wrapLiterals)
        {
            var chain = Invoke("Case");

            if (caseExpression is SearchedCaseExpression searchedCase)
            {
                foreach (var clause in searchedCase.WhenClauses)
                {
                    if (clause is not SearchedWhenClause searchedWhen)
                    {
                        throw new SqExpressSqlTranspilerException($"Unsupported searched CASE clause: {clause.GetType().Name}.");
                    }

                    var whenCondition = this.BuildBooleanExpression(searchedWhen.WhenExpression, context);
                    var thenValue = this.BuildScalarExpression(searchedWhen.ThenExpression, context, wrapLiterals);
                    chain = InvokeMember(InvokeMember(chain, "When", whenCondition), "Then", thenValue);
                }

                var elseValue = searchedCase.ElseExpression != null
                    ? this.BuildScalarExpression(searchedCase.ElseExpression, context, wrapLiterals)
                    : IdentifierName("Null");

                return InvokeMember(chain, "Else", elseValue);
            }

            if (caseExpression is SimpleCaseExpression simpleCase)
            {
                foreach (var clause in simpleCase.WhenClauses)
                {
                    if (clause is not SimpleWhenClause simpleWhen)
                    {
                        throw new SqExpressSqlTranspilerException($"Unsupported simple CASE clause: {clause.GetType().Name}.");
                    }

                    var whenCondition = BinaryExpression(
                        SyntaxKind.EqualsExpression,
                        ParenthesizeIfNeeded(this.BuildScalarExpression(simpleCase.InputExpression, context, wrapLiterals: false)),
                        ParenthesizeIfNeeded(this.BuildScalarExpression(simpleWhen.WhenExpression, context, wrapLiterals: false)));

                    var thenValue = this.BuildScalarExpression(simpleWhen.ThenExpression, context, wrapLiterals);
                    chain = InvokeMember(InvokeMember(chain, "When", whenCondition), "Then", thenValue);
                }

                var elseValue = simpleCase.ElseExpression != null
                    ? this.BuildScalarExpression(simpleCase.ElseExpression, context, wrapLiterals)
                    : IdentifierName("Null");

                return InvokeMember(chain, "Else", elseValue);
            }

            throw new SqExpressSqlTranspilerException($"Unsupported CASE expression type: {caseExpression.GetType().Name}.");
        }

        private ExpressionSyntax BuildFunctionCall(FunctionCall functionCall, TranspileContext context, bool wrapLiterals)
        {
            if (functionCall.CallTarget != null)
            {
                throw new SqExpressSqlTranspilerException("Schema-qualified function calls are not supported yet.");
            }

            if (functionCall.OverClause != null)
            {
                return this.BuildWindowFunctionCall(functionCall, context, wrapLiterals);
            }

            var functionName = functionCall.FunctionName.Value;
            if (this.TryBuildKnownFunctionCall(functionCall, context, wrapLiterals, out var knownFunctionCall))
            {
                return knownFunctionCall;
            }

            if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct)
            {
                if (functionCall.Parameters.Count != 1)
                {
                    throw new SqExpressSqlTranspilerException("DISTINCT aggregate function with multiple arguments is not supported.");
                }

                return Invoke("AggregateFunction", StringLiteral(functionName), LiteralExpression(SyntaxKind.TrueLiteralExpression), this.BuildScalarExpression(functionCall.Parameters[0], context, wrapLiterals));
            }

            var functionArgs = functionCall.Parameters.Select(arg =>
            {
                if (IsStar(arg))
                {
                    throw new SqExpressSqlTranspilerException($"Function '{functionName}' with '*' argument is not supported.");
                }
                return this.BuildScalarExpression(arg, context, wrapLiterals);
            }).ToList();

            return Invoke("ScalarFunctionSys", Prepend(StringLiteral(functionName), functionArgs));
        }

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

            ExpressionSyntax result;
            if (partitions.Count == 0)
            {
                result = orderItems.Count == 0
                    ? InvokeMember(aggregateFunction, "Over")
                    : InvokeMember(aggregateFunction, "OverOrderBy", Prepend(orderItems[0], orderItems.Skip(1).ToList()));
            }
            else
            {
                var partitioned = InvokeMember(aggregateFunction, "OverPartitionBy", Prepend(partitions[0], partitions.Skip(1).ToList()));
                result = orderItems.Count == 0
                    ? InvokeMember(partitioned, "NoOrderBy")
                    : InvokeMember(partitioned, "OrderBy", Prepend(orderItems[0], orderItems.Skip(1).ToList()));
            }

            return result;
        }

        private ExpressionSyntax BuildAggregateWindowFunctionWithFrameHelpers(FunctionCall functionCall, TranspileContext context, bool wrapLiterals)
        {
            var overClause = functionCall.OverClause
                ?? throw new SqExpressSqlTranspilerException("Expected OVER clause for aggregate window function.");

            var frameClause = overClause.WindowFrameClause
                ?? throw new SqExpressSqlTranspilerException("Expected frame clause for aggregate window function.");

            if (frameClause.WindowFrameType == WindowFrameType.Range)
            {
                throw new SqExpressSqlTranspilerException("RANGE window frame is not supported yet. Use ROWS frame.");
            }

            var aggregateFunction = this.BuildAggregateFunctionCall(functionCall, context, wrapLiterals);
            var partitions = this.BuildWindowPartitionExpressions(overClause, context);
            var orderItems = this.BuildWindowOrderByItemExpressions(overClause.OrderByClause, context);

            ExpressionSyntax baseWindow;
            if (partitions.Count == 0)
            {
                baseWindow = orderItems.Count == 0
                    ? InvokeMember(aggregateFunction, "Over")
                    : InvokeMember(aggregateFunction, "OverOrderBy", Prepend(orderItems[0], orderItems.Skip(1).ToList()));
            }
            else
            {
                var partitioned = InvokeMember(aggregateFunction, "OverPartitionBy", Prepend(partitions[0], partitions.Skip(1).ToList()));
                baseWindow = orderItems.Count == 0
                    ? InvokeMember(partitioned, "NoOrderBy")
                    : InvokeMember(partitioned, "OrderBy", Prepend(orderItems[0], orderItems.Skip(1).ToList()));
            }

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

                ExpressionSyntax overBuilder = partitions.Count == 0
                    ? InvokeMember(builderExpression, "OverOrderBy", Prepend(orderItems[0], orderItems.Skip(1).ToList()))
                    : InvokeMember(
                        InvokeMember(builderExpression, "OverPartitionBy", Prepend(partitions[0], partitions.Skip(1).ToList())),
                        "OverOrderBy",
                        Prepend(orderItems[0], orderItems.Skip(1).ToList()));

                if (overClause.WindowFrameClause == null)
                {
                    return InvokeMember(overBuilder, "FrameClauseEmpty");
                }

                if (overClause.WindowFrameClause.WindowFrameType == WindowFrameType.Range)
                {
                    throw new SqExpressSqlTranspilerException("RANGE window frame is not supported yet. Use ROWS frame.");
                }

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

            return partitions.Count == 0
                ? InvokeMember(builderExpression, "OverOrderBy", Prepend(orderItems[0], orderItems.Skip(1).ToList()))
                : InvokeMember(
                    InvokeMember(builderExpression, "OverPartitionBy", Prepend(partitions[0], partitions.Skip(1).ToList())),
                    "OverOrderBy",
                    Prepend(orderItems[0], orderItems.Skip(1).ToList()));
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

        private IReadOnlyList<ExpressionSyntax> BuildFunctionArguments(
            IList<ScalarExpression> parameters,
            TranspileContext context,
            bool wrapLiterals)
        {
            return parameters
                .Select(arg =>
                {
                    if (IsStar(arg))
                    {
                        throw new SqExpressSqlTranspilerException("Function call with '*' argument is not supported in this context.");
                    }

                    return this.BuildScalarExpression(arg, context, wrapLiterals);
                })
                .ToList();
        }

        private ExpressionSyntax BuildExprValueArray(IReadOnlyList<ExpressionSyntax> values)
        {
            return ArrayCreationExpression(
                    ArrayType(ParseTypeName("SqExpress.Syntax.Value.ExprValue"))
                        .WithRankSpecifiers(
                            SingletonList(
                                ArrayRankSpecifier(
                                    SingletonSeparatedList<ExpressionSyntax>(
                                        OmittedArraySizeExpression())))))
                .WithInitializer(
                    InitializerExpression(
                        SyntaxKind.ArrayInitializerExpression,
                        SeparatedList(values)));
        }

        private static bool IsKnownAggregateFunctionName(string normalizedName)
        {
            return normalizedName switch
            {
                "COUNT" => true,
                "MIN" => true,
                "MAX" => true,
                "SUM" => true,
                "AVG" => true,
                _ => false
            };
        }

        private bool TryBuildKnownFunctionCall(
            FunctionCall functionCall,
            TranspileContext context,
            bool wrapLiterals,
            out ExpressionSyntax expression)
        {
            var functionName = functionCall.FunctionName.Value;
            var normalizedName = functionName.ToUpperInvariant();
            expression = default!;

            switch (normalizedName)
            {
                case "COUNT":
                {
                    if (functionCall.Parameters.Count == 1 && IsStar(functionCall.Parameters[0]))
                    {
                        if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct)
                        {
                            throw new SqExpressSqlTranspilerException("COUNT(DISTINCT *) is not valid.");
                        }

                        expression = Invoke("CountOne");
                        return true;
                    }

                    if (functionCall.Parameters.Count != 1)
                    {
                        throw new SqExpressSqlTranspilerException("COUNT supports exactly one argument.");
                    }

                    var countArg = this.BuildScalarExpression(functionCall.Parameters[0], context, wrapLiterals);
                    expression = functionCall.UniqueRowFilter == UniqueRowFilter.Distinct
                        ? Invoke("CountDistinct", countArg)
                        : Invoke("Count", countArg);
                    return true;
                }
                case "MIN":
                case "MAX":
                case "SUM":
                case "AVG":
                {
                    if (functionCall.Parameters.Count != 1)
                    {
                        return false;
                    }

                    var arg = this.BuildScalarExpression(functionCall.Parameters[0], context, wrapLiterals);
                    var distinct = functionCall.UniqueRowFilter == UniqueRowFilter.Distinct;
                    expression = normalizedName switch
                    {
                        "MIN" => Invoke(distinct ? "MinDistinct" : "Min", arg),
                        "MAX" => Invoke(distinct ? "MaxDistinct" : "Max", arg),
                        "SUM" => Invoke(distinct ? "SumDistinct" : "Sum", arg),
                        "AVG" => Invoke(distinct ? "AvgDistinct" : "Avg", arg),
                        _ => throw new SqExpressSqlTranspilerException($"Unsupported known aggregate function: {functionName}.")
                    };
                    return true;
                }
                case "ISNULL":
                {
                    if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct || functionCall.Parameters.Count != 2)
                    {
                        return false;
                    }

                    var arg1 = this.BuildScalarExpression(functionCall.Parameters[0], context, wrapLiterals);
                    var arg2 = this.BuildScalarExpression(functionCall.Parameters[1], context, wrapLiterals);
                    expression = Invoke("IsNull", arg1, arg2);
                    return true;
                }
                case "COALESCE":
                {
                    if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct || functionCall.Parameters.Count < 2)
                    {
                        return false;
                    }

                    var args = functionCall.Parameters
                        .Select(p => this.BuildScalarExpression(p, context, wrapLiterals))
                        .ToList();
                    expression = Invoke("Coalesce", args);
                    return true;
                }
                case "GETDATE":
                {
                    if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct || functionCall.Parameters.Count != 0)
                    {
                        return false;
                    }

                    expression = Invoke("GetDate");
                    return true;
                }
                case "GETUTCDATE":
                {
                    if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct || functionCall.Parameters.Count != 0)
                    {
                        return false;
                    }

                    expression = Invoke("GetUtcDate");
                    return true;
                }
                case "DATEADD":
                {
                    if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct || functionCall.Parameters.Count != 3)
                    {
                        return false;
                    }

                    if (!TryParseDateAddDatePart(functionCall.Parameters[0], out var dateAddDatePart))
                    {
                        return false;
                    }

                    if (!TryExtractInt32Constant(functionCall.Parameters[1], out var number))
                    {
                        return false;
                    }

                    var date = this.BuildScalarExpression(functionCall.Parameters[2], context, wrapLiterals);
                    expression = Invoke(
                        "DateAdd",
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("DateAddDatePart"),
                            IdentifierName(dateAddDatePart.ToString())),
                        LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(number)),
                        date);
                    return true;
                }
                case "DATEDIFF":
                {
                    if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct || functionCall.Parameters.Count != 3)
                    {
                        return false;
                    }

                    if (!TryParseDateDiffDatePart(functionCall.Parameters[0], out var dateDiffDatePart))
                    {
                        return false;
                    }

                    var startDate = this.BuildScalarExpression(functionCall.Parameters[1], context, wrapLiterals);
                    var endDate = this.BuildScalarExpression(functionCall.Parameters[2], context, wrapLiterals);
                    expression = Invoke(
                        "DateDiff",
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName("DateDiffDatePart"),
                            IdentifierName(dateDiffDatePart.ToString())),
                        startDate,
                        endDate);
                    return true;
                }
                default:
                    return false;
            }
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

            if (context.TryResolveColumnSource(columnReference, out var dynamicSource) && dynamicSource.Descriptor == null)
            {
                if (identifiers.Count > 1)
                {
                    return InvokeMember(IdentifierName(dynamicSource.VariableName), "Column", StringLiteral(columnName));
                }
            }

            return Invoke("Column", StringLiteral(columnName));
        }

        private ExpressionSyntax BuildOrderBy(ExpressionWithSortOrder orderByItem, TranspileContext context)
        {
            var expression = this.BuildScalarExpression(orderByItem.Expression, context, wrapLiterals: false);
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

        private static bool IsLiteralOnlyExpression(ScalarExpression expression)
        {
            switch (expression)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.NullLiteral:
                case Microsoft.SqlServer.TransactSql.ScriptDom.StringLiteral:
                case Microsoft.SqlServer.TransactSql.ScriptDom.IntegerLiteral:
                case Microsoft.SqlServer.TransactSql.ScriptDom.NumericLiteral:
                case Microsoft.SqlServer.TransactSql.ScriptDom.MoneyLiteral:
                    return true;
                case UnaryExpression unary:
                    return IsLiteralOnlyExpression(unary.Expression);
                case ParenthesisExpression parenthesis:
                    return IsLiteralOnlyExpression(parenthesis.Expression);
                case BinaryExpression binary:
                    return IsLiteralOnlyExpression(binary.FirstExpression)
                           && IsLiteralOnlyExpression(binary.SecondExpression);
                case CastCall castCall:
                    return IsLiteralOnlyExpression(castCall.Parameter);
                default:
                    return false;
            }
        }

        private static bool TryExtractInt32Constant(ScalarExpression expression, out int value)
        {
            value = 0;
            switch (expression)
            {
                case IntegerLiteral integerLiteral:
                    return int.TryParse(integerLiteral.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
                case UnaryExpression unaryExpression when unaryExpression.UnaryExpressionType == UnaryExpressionType.Negative:
                    if (TryExtractInt32Constant(unaryExpression.Expression, out var negativeInner))
                    {
                        value = -negativeInner;
                        return true;
                    }
                    return false;
                case UnaryExpression unaryExpression when unaryExpression.UnaryExpressionType == UnaryExpressionType.Positive:
                    return TryExtractInt32Constant(unaryExpression.Expression, out value);
                default:
                    return false;
            }
        }

        private static bool TryParseDateAddDatePart(ScalarExpression expression, out DateAddDatePart datePart)
        {
            datePart = default;
            if (!TryGetDatePartToken(expression, out var token))
            {
                return false;
            }

            switch (token)
            {
                case "YEAR":
                case "YY":
                case "YYYY":
                    datePart = DateAddDatePart.Year;
                    return true;
                case "MONTH":
                case "MM":
                case "M":
                    datePart = DateAddDatePart.Month;
                    return true;
                case "DAY":
                case "DD":
                case "D":
                    datePart = DateAddDatePart.Day;
                    return true;
                case "WEEK":
                case "WK":
                case "WW":
                    datePart = DateAddDatePart.Week;
                    return true;
                case "HOUR":
                case "HH":
                    datePart = DateAddDatePart.Hour;
                    return true;
                case "MINUTE":
                case "MI":
                case "N":
                    datePart = DateAddDatePart.Minute;
                    return true;
                case "SECOND":
                case "SS":
                case "S":
                    datePart = DateAddDatePart.Second;
                    return true;
                case "MILLISECOND":
                case "MS":
                    datePart = DateAddDatePart.Millisecond;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryParseDateDiffDatePart(ScalarExpression expression, out DateDiffDatePart datePart)
        {
            datePart = default;
            if (!TryGetDatePartToken(expression, out var token))
            {
                return false;
            }

            switch (token)
            {
                case "YEAR":
                case "YY":
                case "YYYY":
                    datePart = DateDiffDatePart.Year;
                    return true;
                case "MONTH":
                case "MM":
                case "M":
                    datePart = DateDiffDatePart.Month;
                    return true;
                case "DAY":
                case "DD":
                case "D":
                    datePart = DateDiffDatePart.Day;
                    return true;
                case "HOUR":
                case "HH":
                    datePart = DateDiffDatePart.Hour;
                    return true;
                case "MINUTE":
                case "MI":
                case "N":
                    datePart = DateDiffDatePart.Minute;
                    return true;
                case "SECOND":
                case "SS":
                case "S":
                    datePart = DateDiffDatePart.Second;
                    return true;
                case "MILLISECOND":
                case "MS":
                    datePart = DateDiffDatePart.Millisecond;
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetDatePartToken(ScalarExpression expression, out string token)
        {
            token = string.Empty;

            if (expression is ColumnReferenceExpression columnReference
                && columnReference.ColumnType != ColumnType.Wildcard
                && columnReference.MultiPartIdentifier?.Identifiers != null
                && columnReference.MultiPartIdentifier.Identifiers.Count == 1)
            {
                var singleIdentifier = columnReference.MultiPartIdentifier.Identifiers[0].Value;
                if (!string.IsNullOrWhiteSpace(singleIdentifier))
                {
                    token = singleIdentifier.ToUpperInvariant();
                    return true;
                }
            }

            if (expression is IdentifierLiteral identifierLiteral && !string.IsNullOrWhiteSpace(identifierLiteral.Value))
            {
                token = identifierLiteral.Value.ToUpperInvariant();
                return true;
            }

            if (expression is Microsoft.SqlServer.TransactSql.ScriptDom.StringLiteral stringLiteral
                && !string.IsNullOrWhiteSpace(stringLiteral.Value))
            {
                token = stringLiteral.Value.ToUpperInvariant();
                return true;
            }

            return false;
        }

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

        private static QuerySpecification UnwrapQuerySpecification(QueryExpression? queryExpression, string errorMessage)
        {
            if (queryExpression == null)
            {
                throw new SqExpressSqlTranspilerException(errorMessage);
            }

            var current = queryExpression;
            while (current is QueryParenthesisExpression parenthesized)
            {
                current = parenthesized.QueryExpression;
            }

            if (current is QuerySpecification querySpecification)
            {
                return querySpecification;
            }

            throw new SqExpressSqlTranspilerException(errorMessage);
        }

        private static IReadOnlyList<string> ExtractProjectedColumns(QueryExpression queryExpression)
        {
            QuerySpecification specification;
            try
            {
                specification = UnwrapQuerySpecification(
                    queryExpression,
                    "Only SELECT query specifications are supported for projected columns extraction.");
            }
            catch (SqExpressSqlTranspilerException)
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
            private readonly SharedDescriptorState _sharedState;
            private readonly TranspileContext? _parent;
            private readonly List<TableSource> _sources = new();
            private readonly List<LocalDeclarationStatementSyntax> _sourceDeclarations = new();
            private readonly Dictionary<NamedTableReference, TableSource> _namedSourceMap = new(ReferenceEqualityComparer<NamedTableReference>.Instance);
            private readonly Dictionary<QueryDerivedTable, TableSource> _derivedSourceMap = new(ReferenceEqualityComparer<QueryDerivedTable>.Instance);
            private readonly Dictionary<SchemaObjectFunctionTableReference, TableSource> _schemaFunctionSourceMap = new(ReferenceEqualityComparer<SchemaObjectFunctionTableReference>.Instance);
            private readonly Dictionary<BuiltInFunctionTableReference, TableSource> _builtInFunctionSourceMap = new(ReferenceEqualityComparer<BuiltInFunctionTableReference>.Instance);
            private readonly Dictionary<GlobalFunctionTableReference, TableSource> _globalFunctionSourceMap = new(ReferenceEqualityComparer<GlobalFunctionTableReference>.Instance);
            private readonly Dictionary<string, TableSource> _sourceByAlias = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, TableSource?> _sourceByObjectName = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _variableNames;

            public TranspileContext(SqExpressSqlTranspilerOptions options)
                : this(new SharedDescriptorState(), new HashSet<string>(StringComparer.OrdinalIgnoreCase), parent: null)
            {
            }

            private TranspileContext(SharedDescriptorState sharedState, HashSet<string> variableNames, TranspileContext? parent)
            {
                this._sharedState = sharedState;
                this._variableNames = variableNames;
                this._parent = parent;
            }

            public TranspileContext CreateChild(bool shareVariableNames = false, bool inheritSourceResolution = false)
                => new TranspileContext(
                    this._sharedState,
                    shareVariableNames
                        ? this._variableNames
                        : new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    parent: inheritSourceResolution ? this : null);

            public IReadOnlyList<TableDescriptor> Descriptors => this._sharedState.Descriptors;

            public IReadOnlyList<LocalDeclarationStatementSyntax> SourceDeclarations => this._sourceDeclarations;

            public void AbsorbSourceDeclarations(TranspileContext context)
            {
                this._sourceDeclarations.AddRange(context._sourceDeclarations);
            }

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

                    if (this._sharedState.CteDescriptors.ContainsKey(cteName!))
                    {
                        continue;
                    }

                    var descriptor = new TableDescriptor(
                        className: this.CreateClassName(cteName + "Cte", "GeneratedCte"),
                        kind: DescriptorKind.Cte,
                        objectName: cteName!,
                        schemaName: null,
                        databaseName: null,
                        queryExpression: cte.QueryExpression);

                    var columns = cte.Columns.Count > 0
                        ? cte.Columns.Select(i => i.Value).Where(i => !string.IsNullOrWhiteSpace(i)).ToList()
                        : ExtractProjectedColumns(cte.QueryExpression).ToList();

                    foreach (var column in columns)
                    {
                        descriptor.GetOrAddColumn(column, DescriptorColumnKind.Int32);
                    }

                    this._sharedState.CteDescriptors[cteName!] = descriptor;
                    this._sharedState.Descriptors.Add(descriptor);
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
                    && this._sharedState.CteDescriptors.TryGetValue(objectName!, out var cteDescriptor))
                {
                    descriptor = cteDescriptor;
                }
                else
                {
                    descriptor = new TableDescriptor(
                        className: this.CreateClassName(objectName + "Table", "GeneratedTable"),
                        kind: DescriptorKind.Table,
                        objectName: objectName!,
                        schemaName: tableReference.SchemaObject.SchemaIdentifier?.Value,
                        databaseName: tableReference.SchemaObject.DatabaseIdentifier?.Value,
                        queryExpression: null);

                    this._sharedState.Descriptors.Add(descriptor);
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
                    databaseName: null,
                    queryExpression: queryDerivedTable.QueryExpression);

                foreach (var column in ExtractProjectedColumns(queryDerivedTable.QueryExpression))
                {
                    descriptor.GetOrAddColumn(column, DescriptorColumnKind.Int32);
                }

                this._sharedState.Descriptors.Add(descriptor);

                var source = this.RegisterSource(descriptor, alias!, alias, alias!);
                this._derivedSourceMap[queryDerivedTable] = source;
                return source;
            }

            public TableSource GetOrAddSchemaFunctionSource(SchemaObjectFunctionTableReference functionReference, SqExpressSqlTranspiler transpiler)
            {
                if (this._schemaFunctionSourceMap.TryGetValue(functionReference, out var existing))
                {
                    return existing;
                }

                var functionName = functionReference.SchemaObject?.BaseIdentifier?.Value;
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    throw new SqExpressSqlTranspilerException("Table function name cannot be empty.");
                }

                var alias = functionReference.Alias?.Value;
                var sourceExpression = transpiler.BuildTableFunctionSourceExpression(functionReference, this);
                var source = this.RegisterDynamicSource(
                    sourceExpression,
                    alias ?? functionName!,
                    alias,
                    functionName!);

                this._schemaFunctionSourceMap[functionReference] = source;
                return source;
            }

            public TableSource GetOrAddBuiltInFunctionSource(BuiltInFunctionTableReference functionReference, SqExpressSqlTranspiler transpiler)
            {
                if (this._builtInFunctionSourceMap.TryGetValue(functionReference, out var existing))
                {
                    return existing;
                }

                var functionName = functionReference.Name?.Value;
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    throw new SqExpressSqlTranspilerException("Table function name cannot be empty.");
                }

                var alias = functionReference.Alias?.Value;
                var sourceExpression = transpiler.BuildTableFunctionSourceExpression(functionReference, this);
                var source = this.RegisterDynamicSource(
                    sourceExpression,
                    alias ?? functionName!,
                    alias,
                    functionName!);

                this._builtInFunctionSourceMap[functionReference] = source;
                return source;
            }

            public TableSource GetOrAddGlobalFunctionSource(GlobalFunctionTableReference functionReference, SqExpressSqlTranspiler transpiler)
            {
                if (this._globalFunctionSourceMap.TryGetValue(functionReference, out var existing))
                {
                    return existing;
                }

                var functionName = functionReference.Name?.Value;
                if (string.IsNullOrWhiteSpace(functionName))
                {
                    throw new SqExpressSqlTranspilerException("Table function name cannot be empty.");
                }

                var alias = functionReference.Alias?.Value;
                var sourceExpression = transpiler.BuildTableFunctionSourceExpression(functionReference, this);
                var source = this.RegisterDynamicSource(
                    sourceExpression,
                    alias ?? functionName!,
                    alias,
                    functionName!);

                this._globalFunctionSourceMap[functionReference] = source;
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

                if (this._parent != null && this._parent.TryResolveSource(sourceName, out var parentSource))
                {
                    source = parentSource;
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

                if (source.Descriptor == null)
                {
                    return null;
                }

                var descriptorColumn = source.Descriptor.GetOrAddColumn(columnName!, typeHint);
                return new RegisteredDescriptorColumn(source, descriptorColumn);
            }

            public bool TryResolveColumnSource(ColumnReferenceExpression columnReference, out TableSource source)
            {
                source = null!;
                if (columnReference.ColumnType == ColumnType.Wildcard)
                {
                    return false;
                }

                var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
                if (identifiers == null || identifiers.Count < 1)
                {
                    return false;
                }

                if (identifiers.Count > 1)
                {
                    for (int i = identifiers.Count - 2; i >= 0; i--)
                    {
                        var sourceName = identifiers[i].Value;
                        if (!string.IsNullOrWhiteSpace(sourceName) && this.TryResolveSource(sourceName, out var resolved))
                        {
                            source = resolved;
                            return true;
                        }
                    }
                }
                else if (this._sources.Count == 1)
                {
                    source = this._sources[0];
                    return true;
                }

                return false;
            }

            private TableSource RegisterSource(TableDescriptor descriptor, string variableSeed, string? alias, string objectName)
            {
                var source = new TableSource(
                    descriptor,
                    this.CreateVariableName(variableSeed),
                    alias,
                    ObjectCreationExpression(IdentifierName(descriptor.ClassName))
                        .WithArgumentList(
                            ArgumentList(
                                SeparatedList(
                                    !string.IsNullOrWhiteSpace(alias)
                                        ? new[] { Argument(StringLiteral(alias!)) }
                                        : Array.Empty<ArgumentSyntax>()))));
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

            private TableSource RegisterDynamicSource(ExpressionSyntax sourceExpression, string variableSeed, string? alias, string objectName)
            {
                var source = new TableSource(
                    descriptor: null,
                    variableName: this.CreateVariableName(variableSeed),
                    sqlAlias: alias,
                    initializationExpression: sourceExpression);

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
                if (source.Descriptor?.Kind == DescriptorKind.SubQuery && string.IsNullOrWhiteSpace(source.SqlAlias))
                {
                    throw new SqExpressSqlTranspilerException("Derived table alias cannot be empty.");
                }

                return LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator(Identifier(source.VariableName))
                                .WithInitializer(
                                    EqualsValueClause(source.InitializationExpression))));
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
                while (this._sharedState.ClassNames.Contains(candidate))
                {
                    index++;
                    candidate = normalized + index.ToString(CultureInfo.InvariantCulture);
                }

                this._sharedState.ClassNames.Add(candidate);
                return candidate;
            }
        }

        private sealed class SharedDescriptorState
        {
            public List<TableDescriptor> Descriptors { get; } = new();

            public Dictionary<string, TableDescriptor> CteDescriptors { get; } = new(StringComparer.OrdinalIgnoreCase);

            public HashSet<string> ClassNames { get; } = new(StringComparer.OrdinalIgnoreCase);
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
            public TableSource(TableDescriptor? descriptor, string variableName, string? sqlAlias, ExpressionSyntax initializationExpression)
            {
                this.Descriptor = descriptor;
                this.VariableName = variableName;
                this.SqlAlias = sqlAlias;
                this.InitializationExpression = initializationExpression;
            }

            public TableDescriptor? Descriptor { get; }

            public string VariableName { get; }

            public string? SqlAlias { get; }

            public ExpressionSyntax InitializationExpression { get; }
        }

        private sealed class TableDescriptor
        {
            private readonly List<TableDescriptorColumn> _columns = new();
            private readonly Dictionary<string, TableDescriptorColumn> _columnMap = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _propertyNames = new(StringComparer.OrdinalIgnoreCase);

            public TableDescriptor(string className, DescriptorKind kind, string objectName, string? schemaName, string? databaseName, QueryExpression? queryExpression)
            {
                this.ClassName = className;
                this.Kind = kind;
                this.ObjectName = objectName;
                this.SchemaName = schemaName;
                this.DatabaseName = databaseName;
                this.QueryExpression = queryExpression;
            }

            public string ClassName { get; }

            public DescriptorKind Kind { get; }

            public string ObjectName { get; }

            public string? SchemaName { get; }

            public string? DatabaseName { get; }

            public QueryExpression? QueryExpression { get; }

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
