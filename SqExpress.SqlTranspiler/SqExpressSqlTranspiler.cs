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
    public sealed partial class SqExpressSqlTranspiler : ISqExpressSqlTranspiler
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

            if (statement is UpdateStatement updateStatement)
            {
                return this.TranspileUpdate(updateStatement, effectiveOptions);
            }

            if (statement is DeleteStatement deleteStatement)
            {
                return this.TranspileDelete(deleteStatement, effectiveOptions);
            }

            if (statement is InsertStatement insertStatement)
            {
                return this.TranspileInsert(insertStatement, effectiveOptions);
            }

            if (statement is MergeStatement mergeStatement)
            {
                return this.TranspileMerge(mergeStatement, effectiveOptions);
            }

            throw new SqExpressSqlTranspilerException(
                "Only SELECT, INSERT, UPDATE, DELETE and MERGE statements are supported at the moment. " +
                $"Encountered: {statement.GetType().Name}.");
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

            var queryAst = this.BuildQueryAst(doneExpression, selectStatement.QueryExpression, context, options);
            var declarationsAst = this.BuildDeclarationsAst(context, options);

            return new SqExpressTranspileResult(
                statementKind: "SELECT",
                queryAst: queryAst,
                declarationsAst: declarationsAst);
        }

        private SqExpressTranspileResult TranspileInsert(InsertStatement insertStatement, SqExpressSqlTranspilerOptions options)
        {
            if (insertStatement.OptimizerHints.Count > 0)
            {
                throw new SqExpressSqlTranspilerException("INSERT optimizer hints are not supported yet.");
            }

            var specification = insertStatement.InsertSpecification
                ?? throw new SqExpressSqlTranspilerException("INSERT specification is missing.");

            if (specification.TopRowFilter != null)
            {
                throw new SqExpressSqlTranspilerException("INSERT TOP is not supported yet.");
            }

            if (specification.OutputClause != null || specification.OutputIntoClause != null)
            {
                throw new SqExpressSqlTranspilerException("INSERT OUTPUT is not supported yet.");
            }

            if (specification.InsertOption == InsertOption.Over)
            {
                throw new SqExpressSqlTranspilerException("INSERT OVER is not supported.");
            }

            var context = new TranspileContext(options);
            context.RegisterCtes(insertStatement.WithCtesAndXmlNamespaces);
            this.PreRegisterDmlTarget(specification.Target, context, "INSERT");

            var targetSource = this.ResolveDmlTargetSource(specification.Target, context, "INSERT");
            var targetColumns = this.BuildInsertTargetColumns(specification.Columns, context);

            ExpressionSyntax current = Invoke(
                "InsertInto",
                Prepend(IdentifierName(targetSource.VariableName), targetColumns));
            current = this.ApplyInsertSource(current, specification, context);

            var queryAst = this.BuildExecAst(current, context, options);
            var declarationsAst = this.BuildDeclarationsAst(context, options);

            return new SqExpressTranspileResult(
                statementKind: "INSERT",
                queryAst: queryAst,
                declarationsAst: declarationsAst);
        }

        private SqExpressTranspileResult TranspileUpdate(UpdateStatement updateStatement, SqExpressSqlTranspilerOptions options)
        {
            if (updateStatement.OptimizerHints.Count > 0)
            {
                throw new SqExpressSqlTranspilerException("UPDATE optimizer hints are not supported yet.");
            }

            var specification = updateStatement.UpdateSpecification
                ?? throw new SqExpressSqlTranspilerException("UPDATE specification is missing.");

            if (specification.TopRowFilter != null)
            {
                throw new SqExpressSqlTranspilerException("UPDATE TOP is not supported yet.");
            }

            if (specification.OutputClause != null || specification.OutputIntoClause != null)
            {
                throw new SqExpressSqlTranspilerException("UPDATE OUTPUT is not supported yet.");
            }

            if (specification.SetClauses.Count < 1)
            {
                throw new SqExpressSqlTranspilerException("UPDATE SET cannot be empty.");
            }

            var context = new TranspileContext(options);
            context.RegisterCtes(updateStatement.WithCtesAndXmlNamespaces);

            if (specification.FromClause != null)
            {
                if (specification.FromClause.TableReferences.Count != 1)
                {
                    throw new SqExpressSqlTranspilerException("Only one root FROM table-reference is supported.");
                }

                this.PreRegisterTableReferences(specification.FromClause.TableReferences[0], context);
            }

            this.PreRegisterDmlTarget(specification.Target, context, "UPDATE");
            var targetSource = this.ResolveDmlTargetSource(specification.Target, context, "UPDATE");
            ExpressionSyntax current = Invoke("Update", IdentifierName(targetSource.VariableName));
            current = this.ApplyUpdateSetClauses(current, specification.SetClauses, context);

            if (specification.FromClause != null)
            {
                current = this.ApplyTableReference(current, specification.FromClause.TableReferences[0], context, isRoot: true);
            }

            if (specification.WhereClause != null)
            {
                current = InvokeMember(current, "Where", this.BuildBooleanExpression(specification.WhereClause.SearchCondition, context));
            }
            else
            {
                current = InvokeMember(current, "All");
            }

            var queryAst = this.BuildExecAst(current, context, options);
            var declarationsAst = this.BuildDeclarationsAst(context, options);

            return new SqExpressTranspileResult(
                statementKind: "UPDATE",
                queryAst: queryAst,
                declarationsAst: declarationsAst);
        }

        private SqExpressTranspileResult TranspileDelete(DeleteStatement deleteStatement, SqExpressSqlTranspilerOptions options)
        {
            if (deleteStatement.OptimizerHints.Count > 0)
            {
                throw new SqExpressSqlTranspilerException("DELETE optimizer hints are not supported yet.");
            }

            var specification = deleteStatement.DeleteSpecification
                ?? throw new SqExpressSqlTranspilerException("DELETE specification is missing.");

            if (specification.TopRowFilter != null)
            {
                throw new SqExpressSqlTranspilerException("DELETE TOP is not supported yet.");
            }

            if (specification.OutputClause != null || specification.OutputIntoClause != null)
            {
                throw new SqExpressSqlTranspilerException("DELETE OUTPUT is not supported yet.");
            }

            var context = new TranspileContext(options);
            context.RegisterCtes(deleteStatement.WithCtesAndXmlNamespaces);

            if (specification.FromClause != null)
            {
                if (specification.FromClause.TableReferences.Count != 1)
                {
                    throw new SqExpressSqlTranspilerException("Only one root FROM table-reference is supported.");
                }

                this.PreRegisterTableReferences(specification.FromClause.TableReferences[0], context);
            }

            this.PreRegisterDmlTarget(specification.Target, context, "DELETE");
            var targetSource = this.ResolveDmlTargetSource(specification.Target, context, "DELETE");
            ExpressionSyntax current = Invoke("Delete", IdentifierName(targetSource.VariableName));

            if (specification.FromClause != null)
            {
                current = this.ApplyTableReference(current, specification.FromClause.TableReferences[0], context, isRoot: true);
            }

            if (specification.WhereClause != null)
            {
                current = InvokeMember(current, "Where", this.BuildBooleanExpression(specification.WhereClause.SearchCondition, context));
            }
            else
            {
                current = InvokeMember(current, "All");
            }

            var queryAst = this.BuildExecAst(current, context, options);
            var declarationsAst = this.BuildDeclarationsAst(context, options);

            return new SqExpressTranspileResult(
                statementKind: "DELETE",
                queryAst: queryAst,
                declarationsAst: declarationsAst);
        }

        private SqExpressTranspileResult TranspileMerge(MergeStatement mergeStatement, SqExpressSqlTranspilerOptions options)
        {
            if (mergeStatement.OptimizerHints.Count > 0)
            {
                throw new SqExpressSqlTranspilerException("MERGE optimizer hints are not supported yet.");
            }

            var specification = mergeStatement.MergeSpecification
                ?? throw new SqExpressSqlTranspilerException("MERGE specification is missing.");

            if (specification.TopRowFilter != null)
            {
                throw new SqExpressSqlTranspilerException("MERGE TOP is not supported yet.");
            }

            if (specification.OutputClause != null || specification.OutputIntoClause != null)
            {
                throw new SqExpressSqlTranspilerException("MERGE OUTPUT is not supported yet.");
            }

            if (specification.SearchCondition == null)
            {
                throw new SqExpressSqlTranspilerException("MERGE ON condition is required.");
            }

            var context = new TranspileContext(options);
            context.RegisterCtes(mergeStatement.WithCtesAndXmlNamespaces);
            this.PreRegisterTableReferences(specification.TableReference, context);
            this.PreRegisterDmlTarget(specification.Target, context, "MERGE");

            var targetSource = this.ResolveDmlTargetSource(specification.Target, context, "MERGE");
            var targetAlias = specification.TableAlias?.Value;
            if (!string.IsNullOrWhiteSpace(targetAlias))
            {
                context.AddSourceAlias(targetAlias!, targetSource);
            }

            var source = this.ResolveJoinedSource(specification.TableReference, context);
            ExpressionSyntax current = Invoke("MergeInto", IdentifierName(targetSource.VariableName), IdentifierName(source.VariableName));
            current = InvokeMember(current, "On", this.BuildBooleanExpression(specification.SearchCondition, context));
            current = this.ApplyMergeActionClauses(current, specification.ActionClauses, context);
            current = InvokeMember(current, "Done");

            var queryAst = this.BuildExecAst(current, context, options);
            var declarationsAst = this.BuildDeclarationsAst(context, options);

            return new SqExpressTranspileResult(
                statementKind: "MERGE",
                queryAst: queryAst,
                declarationsAst: declarationsAst);
        }

        private CompilationUnitSyntax BuildQueryAst(
            ExpressionSyntax doneExpression,
            QueryExpression queryExpression,
            TranspileContext context,
            SqExpressSqlTranspilerOptions options)
            => this.BuildMethodAst(doneExpression, queryExpression, context, options, "IExprQuery");

        private CompilationUnitSyntax BuildExecAst(ExpressionSyntax doneExpression, TranspileContext context, SqExpressSqlTranspilerOptions options)
            => this.BuildMethodAst(doneExpression, queryExpression: null, context, options, "IExprExec");

        private CompilationUnitSyntax BuildMethodAst(
            ExpressionSyntax doneExpression,
            QueryExpression? queryExpression,
            TranspileContext context,
            SqExpressSqlTranspilerOptions options,
            string returnTypeName)
        {
            var isQueryResult = string.Equals(returnTypeName, "IExprQuery", StringComparison.Ordinal);
            var exposedTableSources = isQueryResult
                ? context.Sources
                    .Where(i => i.Descriptor != null)
                    .ToList()
                : new List<TableSource>();

            var queryVariableName = NormalizeIdentifier(options.QueryVariableName, "query");
            var bodyStatements = new List<RoslynStatementSyntax>();
            bodyStatements.AddRange(context.ParameterDeclarations);
            bodyStatements.AddRange(this.BuildBuildMethodSourceDeclarations(context.SourceDeclarations, exposedTableSources));
            bodyStatements.Add(
                LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator(Identifier(queryVariableName))
                                .WithInitializer(EqualsValueClause(doneExpression)))));
            bodyStatements.Add(ReturnStatement(IdentifierName(queryVariableName)));

            var buildParameters = exposedTableSources
                .Select(source =>
                    Parameter(Identifier(source.VariableName))
                        .WithType(IdentifierName(source.Descriptor!.ClassName))
                        .AddModifiers(Token(SyntaxKind.OutKeyword)))
                .ToArray();

            var methodDeclaration = MethodDeclaration(IdentifierName(returnTypeName), Identifier(options.MethodName))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .WithParameterList(ParameterList(SeparatedList(buildParameters)))
                .WithBody(Block(bodyStatements));

            var members = new List<MemberDeclarationSyntax>();
            members.Add(methodDeclaration);
            if (isQueryResult)
            {
                members.Add(this.BuildAsyncQueryMethod(options, exposedTableSources, queryExpression, context));
            }

            members.AddRange(this.BuildEmbeddedQueryDescriptorClasses(context));

            var classDeclaration = ClassDeclaration(options.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword))
                .AddMembers(members.ToArray());

            var usings = new List<UsingDirectiveSyntax>
            {
                UsingDirective(ParseName("SqExpress")),
                UsingDirective(ParseName("System.Collections.Generic")),
                UsingDirective(ParseName("SqExpress.Syntax.Select")),
                UsingDirective(ParseName("SqExpress.Syntax.Value")),
                UsingDirective(ParseName("SqExpress.Syntax.Functions")),
                UsingDirective(ParseName("SqExpress.Syntax.Functions.Known")),
                UsingDirective(ParseName(options.EffectiveDeclarationsNamespaceName))
            };

            if (options.UseStaticSqQueryBuilderUsing)
            {
                usings.Add(UsingDirective(ParseName("SqExpress.SqQueryBuilder")).WithStaticKeyword(Token(SyntaxKind.StaticKeyword)));
            }

            if (isQueryResult)
            {
                usings.Add(UsingDirective(ParseName("System.Threading.Tasks")));
                usings.Add(UsingDirective(ParseName("SqExpress.DataAccess")));
            }

            var compilationUnit = CompilationUnit()
                .AddUsings(usings.ToArray())
                .AddMembers(
                    NamespaceDeclaration(ParseName(options.NamespaceName))
                        .AddMembers(classDeclaration))
                .NormalizeWhitespace();

            if (!options.UseStaticSqQueryBuilderUsing)
            {
                var excludedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    options.MethodName
                };
                compilationUnit = (CompilationUnitSyntax)new SqQueryBuilderCallQualifier(excludedNames).Visit(compilationUnit);
            }

            return compilationUnit;
        }

        private IReadOnlyList<RoslynStatementSyntax> BuildBuildMethodSourceDeclarations(
            IReadOnlyList<LocalDeclarationStatementSyntax> sourceDeclarations,
            IReadOnlyList<TableSource> exposedTableSources)
        {
            var exposedSourceInitializers = exposedTableSources
                .ToDictionary(i => i.VariableName, i => i.InitializationExpression, StringComparer.OrdinalIgnoreCase);
            var statements = new List<RoslynStatementSyntax>(sourceDeclarations.Count);

            foreach (var sourceDeclaration in sourceDeclarations)
            {
                var declarationName = TryGetLocalDeclarationVariableName(sourceDeclaration);
                if (declarationName != null
                    && exposedSourceInitializers.TryGetValue(declarationName, out var initializationExpression))
                {
                    statements.Add(
                        ExpressionStatement(
                            AssignmentExpression(
                                SyntaxKind.SimpleAssignmentExpression,
                                IdentifierName(declarationName),
                                initializationExpression)));
                    continue;
                }

                statements.Add(sourceDeclaration);
            }

            return statements;
        }

        private MethodDeclarationSyntax BuildAsyncQueryMethod(
            SqExpressSqlTranspilerOptions options,
            IReadOnlyList<TableSource> exposedTableSources,
            QueryExpression? queryExpression,
            TranspileContext context)
        {
            var reservedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var databaseParamName = CreateUniqueIdentifier("database", reservedNames);
            var buildInvocationArguments = new List<ArgumentSyntax>(exposedTableSources.Count);
            foreach (var source in exposedTableSources)
            {
                var outVariableName = CreateUniqueIdentifier(source.VariableName, reservedNames);
                buildInvocationArguments.Add(
                    Argument(
                            DeclarationExpression(
                                IdentifierName("var"),
                                SingleVariableDesignation(Identifier(outVariableName))))
                        .WithRefOrOutKeyword(Token(SyntaxKind.OutKeyword)));
            }

            var buildInvocation = InvocationExpression(
                IdentifierName(options.MethodName),
                ArgumentList(SeparatedList(buildInvocationArguments)));
            var queryInvocation = InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    buildInvocation,
                    IdentifierName("Query")),
                ArgumentList(SingletonSeparatedList(Argument(IdentifierName(databaseParamName)))));

            var recordVariableName = CreateUniqueIdentifier("r", reservedNames);
            var foreachBodyStatements = queryExpression == null
                ? Array.Empty<RoslynStatementSyntax>()
                : this.BuildQueryRecordReadStatements(queryExpression, context, recordVariableName, reservedNames);
            var awaitForEach = ForEachStatement(
                    IdentifierName("var"),
                    Identifier(recordVariableName),
                    queryInvocation,
                    Block(foreachBodyStatements))
                .WithAwaitKeyword(Token(SyntaxKind.AwaitKeyword));

            return MethodDeclaration(IdentifierName("Task"), Identifier("Query"))
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.StaticKeyword), Token(SyntaxKind.AsyncKeyword))
                .WithParameterList(
                    ParameterList(
                        SingletonSeparatedList(
                            Parameter(Identifier(databaseParamName))
                                .WithType(IdentifierName("ISqDatabase")))))
                .WithBody(Block(awaitForEach));
        }

        private RoslynStatementSyntax[] BuildQueryRecordReadStatements(
            QueryExpression queryExpression,
            TranspileContext context,
            string recordVariableName,
            ISet<string> reservedNames)
        {
            if (!TryUnwrapQuerySpecification(queryExpression, out var specification))
            {
                return Array.Empty<RoslynStatementSyntax>();
            }

            var result = new List<RoslynStatementSyntax>();
            foreach (var selectElement in specification.SelectElements)
            {
                if (selectElement is not SelectScalarExpression scalar)
                {
                    continue;
                }

                var alias = TryGetSelectAlias(scalar.ColumnName);
                if (scalar.Expression is ColumnReferenceExpression columnReference)
                {
                    var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
                    var columnSqlName = identifiers != null && identifiers.Count > 0
                        ? identifiers[identifiers.Count - 1].Value
                        : null;

                    if (string.IsNullOrWhiteSpace(alias))
                    {
                        alias = columnSqlName;
                    }

                    if (!string.IsNullOrWhiteSpace(columnSqlName)
                        && context.TryResolveColumnSource(columnReference, out var source)
                        && source.Descriptor != null
                        && source.Descriptor.TryGetColumn(columnSqlName!, out var descriptorColumn))
                    {
                        var sourceColumn = MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            IdentifierName(source.VariableName),
                            IdentifierName(descriptorColumn.PropertyName));

                        ExpressionSyntax readInvocation = string.Equals(alias, descriptorColumn.SqlName, StringComparison.OrdinalIgnoreCase)
                            ? InvokeMember(sourceColumn, "Read", IdentifierName(recordVariableName))
                            : InvokeMember(sourceColumn, "Read", IdentifierName(recordVariableName), StringLiteral(alias ?? descriptorColumn.SqlName));

                        result.Add(CreateVarReadStatement(
                            alias ?? descriptorColumn.PropertyName,
                            readInvocation,
                            reservedNames));
                        continue;
                    }
                }

                if (string.IsNullOrWhiteSpace(alias))
                {
                    continue;
                }

                if (this.TryInferDescriptorColumnKind(scalar.Expression, context, out var inferredKind))
                {
                    var readMethodName = inferredKind switch
                    {
                        DescriptorColumnKind.NVarChar => "GetString",
                        DescriptorColumnKind.Boolean => "GetBoolean",
                        DescriptorColumnKind.Decimal => "GetDecimal",
                        DescriptorColumnKind.DateTime => "GetDateTime",
                        DescriptorColumnKind.DateTimeOffset => "GetDateTimeOffset",
                        DescriptorColumnKind.Guid => "GetGuid",
                        DescriptorColumnKind.ByteArray => "GetByteArray",
                        _ => "GetInt32"
                    };

                    var readInvocation = InvokeMember(
                        IdentifierName(recordVariableName),
                        readMethodName,
                        StringLiteral(alias!));
                    result.Add(CreateVarReadStatement(alias!, readInvocation, reservedNames));
                    continue;
                }

                var fallbackRead = InvokeMember(
                    IdentifierName(recordVariableName),
                    "GetValue",
                    InvokeMember(IdentifierName(recordVariableName), "GetOrdinal", StringLiteral(alias!)));
                result.Add(CreateVarReadStatement(alias!, fallbackRead, reservedNames));
            }

            return result.ToArray();
        }

        private static LocalDeclarationStatementSyntax CreateVarReadStatement(
            string seedName,
            ExpressionSyntax readExpression,
            ISet<string> reservedNames)
        {
            var variableName = CreateUniqueIdentifier(seedName, reservedNames);
            return LocalDeclarationStatement(
                VariableDeclaration(IdentifierName("var"))
                    .AddVariables(
                        VariableDeclarator(Identifier(variableName))
                            .WithInitializer(EqualsValueClause(readExpression))));
        }

        private CompilationUnitSyntax BuildDeclarationsAst(TranspileContext context, SqExpressSqlTranspilerOptions options)
        {
            var members = context.Descriptors
                .Where(i => i.Kind == DescriptorKind.Table)
                .Select(i => (MemberDeclarationSyntax)this.BuildTableDescriptorClass(i))
                .ToArray();

            var usings = new List<UsingDirectiveSyntax>
            {
                UsingDirective(ParseName("System")),
                UsingDirective(ParseName("SqExpress")),
                UsingDirective(ParseName("SqExpress.Syntax.Select")),
                UsingDirective(ParseName("SqExpress.Syntax.Functions")),
                UsingDirective(ParseName("SqExpress.Syntax.Functions.Known"))
            };
            if (options.UseStaticSqQueryBuilderUsing)
            {
                usings.Add(UsingDirective(ParseName("SqExpress.SqQueryBuilder")).WithStaticKeyword(Token(SyntaxKind.StaticKeyword)));
            }

            return CompilationUnit()
                .AddUsings(usings.ToArray())
                .AddMembers(
                    NamespaceDeclaration(ParseName(options.EffectiveDeclarationsNamespaceName))
                        .AddMembers(members))
                .NormalizeWhitespace();
        }

        private IReadOnlyList<ClassDeclarationSyntax> BuildEmbeddedQueryDescriptorClasses(TranspileContext context)
        {
            static int DescriptorOrder(DescriptorKind kind) => kind switch
            {
                DescriptorKind.Cte => 0,
                DescriptorKind.SubQuery => 1,
                _ => 2
            };

            var members = new List<ClassDeclarationSyntax>();
            var builtDescriptors = new HashSet<TableDescriptor>();
            while (true)
            {
                var nextDescriptor = context.Descriptors
                    .Select((descriptor, index) => new { descriptor, index })
                    .Where(i =>
                        !builtDescriptors.Contains(i.descriptor)
                        && i.descriptor.Kind is DescriptorKind.Cte or DescriptorKind.SubQuery)
                    .OrderBy(i => DescriptorOrder(i.descriptor.Kind))
                    .ThenBy(i => i.index)
                    .Select(i => i.descriptor)
                    .FirstOrDefault();

                if (nextDescriptor == null)
                {
                    break;
                }

                members.Add(this.BuildDescriptorClass(nextDescriptor, context));
                builtDescriptors.Add(nextDescriptor);
            }

            return members;
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
            createQueryBody.AddRange(cteQueryContext.ParameterDeclarations);
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
            var subQueryContext = parentContext.CreateChild();
            if (descriptor.QueryExpression == null)
            {
                throw new SqExpressSqlTranspilerException("Derived subquery expression is required.");
            }

            this.PreRegisterQueryExpression(descriptor.QueryExpression, subQueryContext);
            var subQueryExpression = this.BuildQueryExpression(descriptor.QueryExpression, subQueryContext);

            var tableFieldSources = subQueryContext.Sources
                .Where(i => i.Descriptor?.Kind == DescriptorKind.Table)
                .ToList();
            var tableFieldSourceByVariable = tableFieldSources
                .ToDictionary(i => i.VariableName, StringComparer.OrdinalIgnoreCase);
            var projectedTableBindings = this.ExtractSubQueryProjectedTableBindings(descriptor.QueryExpression, subQueryContext)
                .ToDictionary(i => i.OutputColumnSqlName, StringComparer.OrdinalIgnoreCase);

            var constructorStatements = descriptor.Columns
                .Select(column =>
                {
                    var initializeExpression = this.BuildSubQueryColumnInitialization(
                        column,
                        projectedTableBindings,
                        tableFieldSourceByVariable);
                    return (RoslynStatementSyntax)ExpressionStatement(
                        AssignmentExpression(
                            SyntaxKind.SimpleAssignmentExpression,
                            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, ThisExpression(), IdentifierName(column.PropertyName)),
                            initializeExpression));
                })
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

            var aliasToPropertyMap = descriptor.Columns
                .GroupBy(i => i.SqlName, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(i => i.Key, i => i.First().PropertyName, StringComparer.OrdinalIgnoreCase);
            subQueryExpression = ReplaceAsStringAliasWithColumnProperty(subQueryExpression, aliasToPropertyMap);

            var subQueryDoneExpression = InvokeMember(subQueryExpression, "Done");
            var createQueryBody = new List<RoslynStatementSyntax>();
            createQueryBody.AddRange(subQueryContext.ParameterDeclarations);
            var tableFieldNames = new HashSet<string>(tableFieldSources.Select(i => i.VariableName), StringComparer.OrdinalIgnoreCase);
            createQueryBody.AddRange(
                subQueryContext.SourceDeclarations
                    .Where(i =>
                    {
                        var declarationName = TryGetLocalDeclarationVariableName(i);
                        return declarationName == null || !tableFieldNames.Contains(declarationName);
                    }));
            createQueryBody.Add(ReturnStatement(subQueryDoneExpression));

            var createQueryMethod = MethodDeclaration(IdentifierName("IExprSubQuery"), Identifier("CreateQuery"))
                .AddModifiers(Token(SyntaxKind.ProtectedKeyword), Token(SyntaxKind.OverrideKeyword))
                .WithBody(Block(createQueryBody));

            var sourceFields = tableFieldSources
                .Select(i =>
                    (MemberDeclarationSyntax)FieldDeclaration(
                            VariableDeclaration(IdentifierName(i.Descriptor!.ClassName))
                                .AddVariables(
                                    VariableDeclarator(Identifier(i.VariableName))
                                        .WithInitializer(EqualsValueClause(i.InitializationExpression))))
                        .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword)))
                .ToArray();

            var properties = descriptor.Columns
                .Select(column => this.BuildDescriptorProperty(column, tableDescriptor: false))
                .ToArray();

            return ClassDeclaration(descriptor.ClassName)
                .AddModifiers(Token(SyntaxKind.PublicKeyword), Token(SyntaxKind.SealedKeyword))
                .WithBaseList(BaseList(SingletonSeparatedList<BaseTypeSyntax>(SimpleBaseType(IdentifierName("DerivedTableBase")))))
                .AddMembers(sourceFields)
                .AddMembers(properties)
                .AddMembers(constructor)
                .AddMembers(createQueryMethod);
        }

        private ExpressionSyntax BuildSubQueryColumnInitialization(
            TableDescriptorColumn targetColumn,
            IReadOnlyDictionary<string, SubQueryProjectedTableBinding> projectedTableBindings,
            IReadOnlyDictionary<string, TableSource> tableFieldSourceByVariable)
        {
            if (projectedTableBindings.TryGetValue(targetColumn.SqlName, out var binding)
                && tableFieldSourceByVariable.TryGetValue(binding.SourceVariableName, out _))
            {
                var sourceColumn = binding.SourceColumn;
                if (targetColumn.IsNullable && !sourceColumn.IsNullable)
                {
                    return this.BuildCreateColumnExpression(targetColumn, tableDescriptor: false);
                }

                ExpressionSyntax sourceExpression = MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MemberAccessExpression(
                        SyntaxKind.SimpleMemberAccessExpression,
                        ThisExpression(),
                        IdentifierName(binding.SourceVariableName)),
                    IdentifierName(sourceColumn.PropertyName));

                if (!string.Equals(sourceColumn.SqlName, targetColumn.SqlName, StringComparison.OrdinalIgnoreCase))
                {
                    sourceExpression = InvokeMember(sourceExpression, "WithColumnName", StringLiteral(targetColumn.SqlName));
                }

                return InvokeMember(sourceExpression, "AddToDerivedTable", ThisExpression());
            }

            return this.BuildCreateColumnExpression(targetColumn, tableDescriptor: false);
        }

        private PropertyDeclarationSyntax BuildDescriptorProperty(TableDescriptorColumn column, bool tableDescriptor)
        {
            string typeName;
            if (tableDescriptor)
            {
                typeName = column.Kind switch
                {
                    DescriptorColumnKind.NVarChar => column.IsNullable ? "NullableStringTableColumn" : "StringTableColumn",
                    DescriptorColumnKind.Boolean => column.IsNullable ? "NullableBooleanTableColumn" : "BooleanTableColumn",
                    DescriptorColumnKind.Decimal => column.IsNullable ? "NullableDecimalTableColumn" : "DecimalTableColumn",
                    DescriptorColumnKind.DateTime => column.IsNullable ? "NullableDateTimeTableColumn" : "DateTimeTableColumn",
                    DescriptorColumnKind.DateTimeOffset => column.IsNullable ? "NullableDateTimeOffsetTableColumn" : "DateTimeOffsetTableColumn",
                    DescriptorColumnKind.Guid => column.IsNullable ? "NullableGuidTableColumn" : "GuidTableColumn",
                    DescriptorColumnKind.ByteArray => column.IsNullable ? "NullableByteArrayTableColumn" : "ByteArrayTableColumn",
                    _ => column.IsNullable ? "NullableInt32TableColumn" : "Int32TableColumn"
                };
            }
            else
            {
                typeName = column.Kind switch
                {
                    DescriptorColumnKind.NVarChar => column.IsNullable ? "NullableStringCustomColumn" : "StringCustomColumn",
                    DescriptorColumnKind.Boolean => column.IsNullable ? "NullableBooleanCustomColumn" : "BooleanCustomColumn",
                    DescriptorColumnKind.Decimal => column.IsNullable ? "NullableDecimalCustomColumn" : "DecimalCustomColumn",
                    DescriptorColumnKind.DateTime => column.IsNullable ? "NullableDateTimeCustomColumn" : "DateTimeCustomColumn",
                    DescriptorColumnKind.DateTimeOffset => column.IsNullable ? "NullableDateTimeOffsetCustomColumn" : "DateTimeOffsetCustomColumn",
                    DescriptorColumnKind.Guid => column.IsNullable ? "NullableGuidCustomColumn" : "GuidCustomColumn",
                    DescriptorColumnKind.ByteArray => column.IsNullable ? "NullableByteArrayCustomColumn" : "ByteArrayCustomColumn",
                    _ => column.IsNullable ? "NullableInt32CustomColumn" : "Int32CustomColumn"
                };
            }

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
            return column.Kind switch
            {
                DescriptorColumnKind.NVarChar => tableDescriptor
                    ? column.IsNullable
                        ? InvokeMember(
                            ThisExpression(),
                            "CreateNullableStringColumn",
                            StringLiteral(column.SqlName),
                            column.StringLength.HasValue
                                ? LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(column.StringLength.Value))
                                : LiteralExpression(SyntaxKind.NullLiteralExpression),
                            LiteralExpression(SyntaxKind.TrueLiteralExpression))
                        : InvokeMember(
                            ThisExpression(),
                            "CreateStringColumn",
                            StringLiteral(column.SqlName),
                            column.StringLength.HasValue
                                ? LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(column.StringLength.Value))
                                : LiteralExpression(SyntaxKind.NullLiteralExpression),
                            LiteralExpression(SyntaxKind.TrueLiteralExpression))
                    : column.IsNullable
                        ? InvokeMember(ThisExpression(), "CreateNullableStringColumn", StringLiteral(column.SqlName))
                        : InvokeMember(ThisExpression(), "CreateStringColumn", StringLiteral(column.SqlName)),
                DescriptorColumnKind.Boolean => column.IsNullable
                    ? InvokeMember(ThisExpression(), "CreateNullableBooleanColumn", StringLiteral(column.SqlName))
                    : InvokeMember(ThisExpression(), "CreateBooleanColumn", StringLiteral(column.SqlName)),
                DescriptorColumnKind.Decimal => column.IsNullable
                    ? InvokeMember(ThisExpression(), "CreateNullableDecimalColumn", StringLiteral(column.SqlName))
                    : InvokeMember(ThisExpression(), "CreateDecimalColumn", StringLiteral(column.SqlName)),
                DescriptorColumnKind.DateTime => column.IsNullable
                    ? InvokeMember(ThisExpression(), "CreateNullableDateTimeColumn", StringLiteral(column.SqlName))
                    : InvokeMember(ThisExpression(), "CreateDateTimeColumn", StringLiteral(column.SqlName)),
                DescriptorColumnKind.DateTimeOffset => column.IsNullable
                    ? InvokeMember(ThisExpression(), "CreateNullableDateTimeOffsetColumn", StringLiteral(column.SqlName))
                    : InvokeMember(ThisExpression(), "CreateDateTimeOffsetColumn", StringLiteral(column.SqlName)),
                DescriptorColumnKind.Guid => column.IsNullable
                    ? InvokeMember(ThisExpression(), "CreateNullableGuidColumn", StringLiteral(column.SqlName))
                    : InvokeMember(ThisExpression(), "CreateGuidColumn", StringLiteral(column.SqlName)),
                DescriptorColumnKind.ByteArray => tableDescriptor
                    ? column.IsNullable
                        ? InvokeMember(ThisExpression(), "CreateNullableByteArrayColumn", StringLiteral(column.SqlName), LiteralExpression(SyntaxKind.NullLiteralExpression))
                        : InvokeMember(ThisExpression(), "CreateByteArrayColumn", StringLiteral(column.SqlName), LiteralExpression(SyntaxKind.NullLiteralExpression))
                    : column.IsNullable
                        ? InvokeMember(ThisExpression(), "CreateNullableByteArrayColumn", StringLiteral(column.SqlName))
                        : InvokeMember(ThisExpression(), "CreateByteArrayColumn", StringLiteral(column.SqlName)),
                _ => column.IsNullable
                    ? InvokeMember(ThisExpression(), "CreateNullableInt32Column", StringLiteral(column.SqlName))
                    : InvokeMember(ThisExpression(), "CreateInt32Column", StringLiteral(column.SqlName))
            };
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
                if (!distinct && IsSelectOneProjection(specification))
                {
                    current = Invoke("SelectOne");
                }
                else
                {
                    current = Invoke(distinct ? "SelectDistinct" : "Select", selectArguments);
                }
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

                current = InvokeMember(current, "GroupBy", groupByColumns);
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

            if (tableReference is InlineDerivedTable inlineDerivedTable)
            {
                context.GetOrAddInlineDerivedSource(inlineDerivedTable, this);
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

        private void PreRegisterDmlTarget(TableReference targetReference, TranspileContext context, string operationName)
        {
            if (targetReference is not NamedTableReference namedTable)
            {
                throw new SqExpressSqlTranspilerException($"{operationName} target must be a named table.");
            }

            var alias = namedTable.Alias?.Value;
            if (!string.IsNullOrWhiteSpace(alias) && context.TryResolveSource(alias!, out _))
            {
                return;
            }

            var schemaObject = namedTable.SchemaObject
                ?? throw new SqExpressSqlTranspilerException($"{operationName} target table is missing.");
            var objectName = schemaObject.BaseIdentifier?.Value;
            if (schemaObject.DatabaseIdentifier == null
                && schemaObject.SchemaIdentifier == null
                && !string.IsNullOrWhiteSpace(objectName)
                && context.TryResolveSource(objectName!, out _))
            {
                return;
            }

            context.GetOrAddNamedSource(namedTable);
        }

        private TableSource ResolveDmlTargetSource(TableReference targetReference, TranspileContext context, string operationName)
        {
            if (targetReference is not NamedTableReference namedTable)
            {
                throw new SqExpressSqlTranspilerException($"{operationName} target must be a named table.");
            }

            var schemaObject = namedTable.SchemaObject
                ?? throw new SqExpressSqlTranspilerException($"{operationName} target table is missing.");
            var objectName = schemaObject.BaseIdentifier?.Value;
            if (string.IsNullOrWhiteSpace(objectName))
            {
                throw new SqExpressSqlTranspilerException($"{operationName} target table name is missing.");
            }

            var alias = namedTable.Alias?.Value;
            var isUnqualifiedTarget = schemaObject.DatabaseIdentifier == null && schemaObject.SchemaIdentifier == null;
            TableSource source;

            if (!string.IsNullOrWhiteSpace(alias) && context.TryResolveSource(alias!, out var byAlias))
            {
                source = byAlias;
            }
            else if (isUnqualifiedTarget && context.TryResolveSource(objectName!, out var byName))
            {
                source = byName;
            }
            else
            {
                source = context.GetOrAddNamedSource(namedTable);
            }

            if (source.Descriptor?.Kind != DescriptorKind.Table)
            {
                throw new SqExpressSqlTranspilerException($"{operationName} target must resolve to a regular table source.");
            }

            return source;
        }

        private List<ExpressionSyntax> BuildInsertTargetColumns(IList<ColumnReferenceExpression> columns, TranspileContext context)
        {
            if (columns.Count < 1)
            {
                throw new SqExpressSqlTranspilerException(
                    "INSERT without target column list is not supported yet.");
            }

            return columns
                .Select(i => this.BuildColumnExpression(i, context))
                .ToList();
        }

        private ExpressionSyntax ApplyInsertSource(ExpressionSyntax current, InsertSpecification specification, TranspileContext context)
        {
            var source = specification.InsertSource
                ?? throw new SqExpressSqlTranspilerException("INSERT source is missing.");

            switch (source)
            {
                case ValuesInsertSource valuesSource:
                    if (valuesSource.IsDefaultValues)
                    {
                        throw new SqExpressSqlTranspilerException("INSERT DEFAULT VALUES is not supported yet.");
                    }

                    if (valuesSource.RowValues.Count < 1)
                    {
                        throw new SqExpressSqlTranspilerException("INSERT VALUES cannot be empty.");
                    }

                    if (specification.Columns.Count < 1)
                    {
                        throw new SqExpressSqlTranspilerException(
                            "INSERT VALUES requires explicit target columns.");
                    }

                    foreach (var row in valuesSource.RowValues)
                    {
                        if (row.ColumnValues.Count != specification.Columns.Count)
                        {
                            throw new SqExpressSqlTranspilerException(
                                "INSERT column count does not match VALUES item count.");
                        }

                        var values = new List<ExpressionSyntax>();
                        for (var i = 0; i < row.ColumnValues.Count; i++)
                        {
                            var value = row.ColumnValues[i];
                            if (value is DefaultLiteral)
                            {
                                throw new SqExpressSqlTranspilerException(
                                    "INSERT VALUES with DEFAULT item is not supported yet.");
                            }

                            this.ApplyInsertValueTypeHints(specification.Columns[i], value, context);
                            values.Add(this.BuildScalarExpression(value, context, wrapLiterals: false));
                        }

                        current = InvokeMember(current, "Values", values);
                    }

                    return InvokeMember(current, "DoneWithValues");
                case SelectInsertSource selectSource:
                    if (specification.Columns.Count < 1)
                    {
                        throw new SqExpressSqlTranspilerException(
                            "INSERT...SELECT requires explicit target columns.");
                    }

                    if (selectSource.Select == null)
                    {
                        throw new SqExpressSqlTranspilerException("INSERT SELECT source is missing.");
                    }

                    this.PreRegisterQueryExpression(selectSource.Select, context);
                    return InvokeMember(current, "From", this.BuildQueryExpression(selectSource.Select, context));
                case ExecuteInsertSource:
                    throw new SqExpressSqlTranspilerException("INSERT EXEC source is not supported yet.");
                default:
                    throw new SqExpressSqlTranspilerException($"Unsupported INSERT source: {source.GetType().Name}.");
            }
        }

        private void ApplyInsertValueTypeHints(ColumnReferenceExpression targetColumn, ScalarExpression value, TranspileContext context)
        {
            if (this.TryExtractVariableReference(value, out var variableName, out var kindHint))
            {
                context.RegisterSqlVariable(variableName, kindHint);
                context.RegisterSqlVariable(variableName, this.InferScalarVariableKind(targetColumn, context));
            }

            if (this.TryInferDescriptorColumnKind(value, context, out var targetHint))
            {
                context.MarkColumnAsKind(targetColumn, targetHint);
            }

            if (value is ColumnReferenceExpression sourceColumn
                && this.TryInferDescriptorColumnKind(targetColumn, context, out var sourceHint))
            {
                context.MarkColumnAsKind(sourceColumn, sourceHint);
            }
        }

        private ExpressionSyntax ApplyUpdateSetClauses(ExpressionSyntax current, IList<SetClause> setClauses, TranspileContext context)
        {
            foreach (var setClause in setClauses)
            {
                if (setClause is not AssignmentSetClause assignment)
                {
                    throw new SqExpressSqlTranspilerException($"Unsupported UPDATE SET clause: {setClause.GetType().Name}.");
                }

                if (assignment.AssignmentKind != AssignmentKind.Equals)
                {
                    throw new SqExpressSqlTranspilerException($"Unsupported assignment kind in SET clause: {assignment.AssignmentKind}.");
                }

                if (assignment.Variable != null)
                {
                    throw new SqExpressSqlTranspilerException("SET variable assignment is not supported.");
                }

                if (this.TryExtractVariableReference(assignment.NewValue, out var variableName, out var kindHint))
                {
                    context.RegisterSqlVariable(variableName, kindHint);
                    context.RegisterSqlVariable(variableName, this.InferScalarVariableKind(assignment.Column, context));
                }

                if (this.TryInferDescriptorColumnKind(assignment.NewValue, context, out var leftHint))
                {
                    context.MarkColumnAsKind(assignment.Column, leftHint);
                }

                if (assignment.NewValue is ColumnReferenceExpression rightColumn
                    && this.TryInferDescriptorColumnKind(assignment.Column, context, out var rightHint))
                {
                    context.MarkColumnAsKind(rightColumn, rightHint);
                }

                var column = this.BuildColumnExpression(assignment.Column, context);
                var value = this.BuildScalarExpression(assignment.NewValue, context, wrapLiterals: false);
                current = InvokeMember(current, "Set", column, value);
            }

            return current;
        }

        private ExpressionSyntax ApplyMergeActionClauses(ExpressionSyntax current, IList<MergeActionClause> actionClauses, TranspileContext context)
        {
            MergeActionClause? whenMatched = null;
            MergeActionClause? whenNotMatchedByTarget = null;
            MergeActionClause? whenNotMatchedBySource = null;

            foreach (var actionClause in actionClauses)
            {
                switch (actionClause.Condition)
                {
                    case MergeCondition.Matched:
                        if (whenMatched != null)
                        {
                            throw new SqExpressSqlTranspilerException("Multiple WHEN MATCHED clauses are not supported yet.");
                        }

                        whenMatched = actionClause;
                        break;
                    case MergeCondition.NotMatched:
                    case MergeCondition.NotMatchedByTarget:
                        if (whenNotMatchedByTarget != null)
                        {
                            throw new SqExpressSqlTranspilerException("Multiple WHEN NOT MATCHED BY TARGET clauses are not supported yet.");
                        }

                        whenNotMatchedByTarget = actionClause;
                        break;
                    case MergeCondition.NotMatchedBySource:
                        if (whenNotMatchedBySource != null)
                        {
                            throw new SqExpressSqlTranspilerException("Multiple WHEN NOT MATCHED BY SOURCE clauses are not supported yet.");
                        }

                        whenNotMatchedBySource = actionClause;
                        break;
                    default:
                        throw new SqExpressSqlTranspilerException($"Unsupported MERGE action condition: {actionClause.Condition}.");
                }
            }

            if (whenMatched != null)
            {
                current = this.ApplyMergeActionClause(current, whenMatched, context);
            }

            if (whenNotMatchedByTarget != null)
            {
                current = this.ApplyMergeActionClause(current, whenNotMatchedByTarget, context);
            }

            if (whenNotMatchedBySource != null)
            {
                current = this.ApplyMergeActionClause(current, whenNotMatchedBySource, context);
            }

            return current;
        }

        private ExpressionSyntax ApplyMergeActionClause(ExpressionSyntax current, MergeActionClause actionClause, TranspileContext context)
        {
            switch (actionClause.Condition)
            {
                case MergeCondition.Matched:
                    current = actionClause.SearchCondition != null
                        ? InvokeMember(current, "WhenMatchedAnd", this.BuildBooleanExpression(actionClause.SearchCondition, context))
                        : InvokeMember(current, "WhenMatched");

                    return actionClause.Action switch
                    {
                        UpdateMergeAction updateAction => this.ApplyUpdateSetClauses(
                            InvokeMember(current, "ThenUpdate"),
                            updateAction.SetClauses,
                            context),
                        DeleteMergeAction => InvokeMember(current, "ThenDelete"),
                        _ => throw new SqExpressSqlTranspilerException(
                            $"Unsupported WHEN MATCHED action: {actionClause.Action.GetType().Name}.")
                    };
                case MergeCondition.NotMatched:
                case MergeCondition.NotMatchedByTarget:
                    current = actionClause.SearchCondition != null
                        ? InvokeMember(current, "WhenNotMatchedByTargetAnd", this.BuildBooleanExpression(actionClause.SearchCondition, context))
                        : InvokeMember(current, "WhenNotMatchedByTarget");

                    if (actionClause.Action is not InsertMergeAction insertAction)
                    {
                        throw new SqExpressSqlTranspilerException(
                            $"Unsupported WHEN NOT MATCHED BY TARGET action: {actionClause.Action.GetType().Name}.");
                    }

                    return this.ApplyMergeInsertAction(current, insertAction, context);
                case MergeCondition.NotMatchedBySource:
                    current = actionClause.SearchCondition != null
                        ? InvokeMember(current, "WhenNotMatchedBySourceAnd", this.BuildBooleanExpression(actionClause.SearchCondition, context))
                        : InvokeMember(current, "WhenNotMatchedBySource");

                    return actionClause.Action switch
                    {
                        UpdateMergeAction updateAction => this.ApplyUpdateSetClauses(
                            InvokeMember(current, "ThenUpdate"),
                            updateAction.SetClauses,
                            context),
                        DeleteMergeAction => InvokeMember(current, "ThenDelete"),
                        _ => throw new SqExpressSqlTranspilerException(
                            $"Unsupported WHEN NOT MATCHED BY SOURCE action: {actionClause.Action.GetType().Name}.")
                    };
                default:
                    throw new SqExpressSqlTranspilerException($"Unsupported MERGE action condition: {actionClause.Condition}.");
            }
        }

        private ExpressionSyntax ApplyMergeInsertAction(ExpressionSyntax current, InsertMergeAction insertAction, TranspileContext context)
        {
            var source = insertAction.Source
                ?? throw new SqExpressSqlTranspilerException("MERGE INSERT source is missing.");

            if (this.IsMergeInsertDefaultValues(insertAction))
            {
                return InvokeMember(current, "ThenInsertDefaultValues");
            }

            if (insertAction.Columns.Count < 1)
            {
                throw new SqExpressSqlTranspilerException("MERGE INSERT column list cannot be empty unless DEFAULT VALUES is used.");
            }

            if (source.RowValues.Count != 1)
            {
                throw new SqExpressSqlTranspilerException("MERGE INSERT currently supports exactly one VALUES row.");
            }

            var values = source.RowValues[0].ColumnValues;
            if (values.Count != insertAction.Columns.Count)
            {
                throw new SqExpressSqlTranspilerException("MERGE INSERT column count does not match VALUES item count.");
            }

            current = InvokeMember(current, "ThenInsert");
            for (var i = 0; i < insertAction.Columns.Count; i++)
            {
                var columnName = this.BuildMergeInsertColumnName(insertAction.Columns[i]);
                var value = this.BuildScalarExpression(values[i], context, wrapLiterals: false);
                current = InvokeMember(current, "Set", columnName, value);
            }

            return current;
        }

        private ExpressionSyntax BuildMergeInsertColumnName(ColumnReferenceExpression columnReference)
        {
            if (columnReference.ColumnType == ColumnType.Wildcard)
            {
                throw new SqExpressSqlTranspilerException("MERGE INSERT column name cannot be wildcard.");
            }

            var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
            if (identifiers == null || identifiers.Count < 1)
            {
                throw new SqExpressSqlTranspilerException("MERGE INSERT column reference does not contain identifiers.");
            }

            var columnName = identifiers[identifiers.Count - 1].Value;
            if (string.IsNullOrWhiteSpace(columnName))
            {
                throw new SqExpressSqlTranspilerException("MERGE INSERT column name cannot be empty.");
            }

            return InvokeMember(IdentifierName("CustomColumnFactory"), "Any", StringLiteral(columnName!));
        }

        private bool IsMergeInsertDefaultValues(InsertMergeAction insertAction)
            => insertAction.Columns.Count == 0
               && insertAction.Source != null
               && insertAction.Source.RowValues.Count == 0;

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

            if (tableReference is InlineDerivedTable inlineDerivedTable)
            {
                if (!isRoot)
                {
                    throw new SqExpressSqlTranspilerException("Unexpected standalone table reference in joined table tree.");
                }

                var source = context.GetOrAddInlineDerivedSource(inlineDerivedTable, this);
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

        private ExpressionSyntax BuildInlineDerivedTableSourceExpression(InlineDerivedTable inlineDerivedTable, TranspileContext context)
        {
            if (inlineDerivedTable.RowValues.Count < 1)
            {
                throw new SqExpressSqlTranspilerException("VALUES table constructor cannot be empty.");
            }

            var exprValueType = ParseTypeName("ExprValue");
            var readOnlyExprValueListType = ParseTypeName("IReadOnlyList<ExprValue>");
            var rowInitializers = inlineDerivedTable.RowValues
                .Select(row =>
                    (ExpressionSyntax)ArrayCreationExpression(
                            ArrayType(exprValueType)
                                .WithRankSpecifiers(
                                    SingletonList(
                                        ArrayRankSpecifier(
                                            SingletonSeparatedList<ExpressionSyntax>(
                                                OmittedArraySizeExpression())))))
                        .WithInitializer(
                            InitializerExpression(
                                SyntaxKind.ArrayInitializerExpression,
                                SeparatedList(
                                    row.ColumnValues
                                        .Select(value => this.BuildScalarExpression(value, context, wrapLiterals: true))
                                        .ToArray()))))
                .ToArray();

            var valuesArg = ImplicitArrayCreationExpression(
                InitializerExpression(
                    SyntaxKind.ArrayInitializerExpression,
                    SeparatedList(
                        rowInitializers
                            .Select(row => (ExpressionSyntax)CastExpression(readOnlyExprValueListType, row))
                            .ToArray())));

            var valuesTable = Invoke(
                "Values",
                valuesArg);

            var alias = inlineDerivedTable.Alias?.Value;
            if (string.IsNullOrWhiteSpace(alias))
            {
                throw new SqExpressSqlTranspilerException("VALUES derived table alias cannot be empty.");
            }

            var columnNames = inlineDerivedTable.Columns
                .Select(i => i.Value)
                .Where(i => !string.IsNullOrWhiteSpace(i))
                .ToList();

            if (columnNames.Count < 1)
            {
                var firstRowColumnCount = inlineDerivedTable.RowValues[0].ColumnValues.Count;
                for (var i = 0; i < firstRowColumnCount; i++)
                {
                    columnNames.Add("C" + (i + 1).ToString(CultureInfo.InvariantCulture));
                }
            }

            return InvokeMember(
                valuesTable,
                "As",
                Prepend(
                    StringLiteral(alias!),
                    columnNames.Select(StringLiteral).ToList()));
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

            if (tableReference is InlineDerivedTable inlineDerivedTable)
            {
                return context.GetOrAddInlineDerivedSource(inlineDerivedTable, this);
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

        private ExpressionSyntax BuildScalarExpression(ScalarExpression expression, TranspileContext context, bool wrapLiterals)
        {
            this.ApplyOperatorAndFunctionTypeHints(expression, context);

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

            if (expression is VariableReference variableReference)
            {
                var variable = context.RegisterSqlVariable(variableReference.Name, SqlVariableKind.UnknownScalar);
                return Invoke("Literal", IdentifierName(variable.VariableName));
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

            if (expression is BinaryLiteral binaryLiteral)
            {
                var binaryValue = ParseBinaryLiteral(binaryLiteral.Value);
                return wrapLiterals
                    ? Invoke("Literal", binaryValue)
                    : binaryValue;
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

        private void ApplyOperatorAndFunctionTypeHints(ScalarExpression expression, TranspileContext context)
        {
            if (expression is BinaryExpression binaryExpression)
            {
                if (binaryExpression.BinaryExpressionType == BinaryExpressionType.Add)
                {
                    var leftIsString = this.TryInferDescriptorColumnKind(binaryExpression.FirstExpression, context, out var leftKind)
                        && leftKind == DescriptorColumnKind.NVarChar
                        || binaryExpression.FirstExpression is StringLiteral;
                    var rightIsString = this.TryInferDescriptorColumnKind(binaryExpression.SecondExpression, context, out var rightKind)
                        && rightKind == DescriptorColumnKind.NVarChar
                        || binaryExpression.SecondExpression is StringLiteral;

                    if (leftIsString || rightIsString)
                    {
                        this.MarkColumnReferencesAsKind(binaryExpression.FirstExpression, DescriptorColumnKind.NVarChar, context);
                        this.MarkColumnReferencesAsKind(binaryExpression.SecondExpression, DescriptorColumnKind.NVarChar, context);
                        return;
                    }
                }

                if (IsArithmeticBinary(binaryExpression.BinaryExpressionType))
                {
                    var numericKind = this.ShouldPreferDecimal(binaryExpression.FirstExpression, binaryExpression.SecondExpression, context)
                        ? DescriptorColumnKind.Decimal
                        : DescriptorColumnKind.Int32;
                    this.MarkColumnReferencesAsKind(binaryExpression.FirstExpression, numericKind, context);
                    this.MarkColumnReferencesAsKind(binaryExpression.SecondExpression, numericKind, context);
                }

                return;
            }

            if (expression is FunctionCall functionCall)
            {
                var functionName = functionCall.FunctionName.Value.ToUpperInvariant();
                switch (functionName)
                {
                    case "LEN":
                    case "LOWER":
                    case "UPPER":
                    case "LTRIM":
                    case "RTRIM":
                    case "TRIM":
                    case "SUBSTRING":
                    case "LEFT":
                    case "RIGHT":
                    case "REPLACE":
                    case "CONCAT":
                    case "CHARINDEX":
                    case "PATINDEX":
                        foreach (var parameter in functionCall.Parameters)
                        {
                            this.MarkColumnReferencesAsKind(parameter, DescriptorColumnKind.NVarChar, context);
                        }
                        return;
                    case "DATEADD":
                        if (functionCall.Parameters.Count == 3)
                        {
                            this.MarkColumnReferencesAsKind(functionCall.Parameters[2], DescriptorColumnKind.DateTime, context);
                        }

                        return;
                    case "DATEDIFF":
                        if (functionCall.Parameters.Count == 3)
                        {
                            this.MarkColumnReferencesAsKind(functionCall.Parameters[1], DescriptorColumnKind.DateTime, context);
                            this.MarkColumnReferencesAsKind(functionCall.Parameters[2], DescriptorColumnKind.DateTime, context);
                        }

                        return;
                    case "YEAR":
                    case "MONTH":
                    case "DAY":
                    case "HOUR":
                    case "MINUTE":
                    case "SECOND":
                        if (functionCall.Parameters.Count >= 1)
                        {
                            this.MarkColumnReferencesAsKind(functionCall.Parameters[0], DescriptorColumnKind.DateTime, context);
                        }

                        return;
                    case "ABS":
                    case "ROUND":
                    case "CEILING":
                    case "FLOOR":
                    case "POWER":
                    case "SQRT":
                        foreach (var parameter in functionCall.Parameters)
                        {
                            this.MarkColumnReferencesAsKind(parameter, DescriptorColumnKind.Decimal, context);
                        }

                        return;
                }
            }
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
            context.AbsorbSqlVariables(subQueryContext);

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
                SqlDataTypeOption.TinyInt => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Int16")),
                SqlDataTypeOption.Bit => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Boolean")),
                SqlDataTypeOption.Decimal => InvokeMember(IdentifierName("SqlType"), "Decimal"),
                SqlDataTypeOption.Numeric => InvokeMember(IdentifierName("SqlType"), "Decimal"),
                SqlDataTypeOption.Money => InvokeMember(IdentifierName("SqlType"), "Decimal"),
                SqlDataTypeOption.SmallMoney => InvokeMember(IdentifierName("SqlType"), "Decimal"),
                SqlDataTypeOption.Float => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Double")),
                SqlDataTypeOption.DateTime => InvokeMember(IdentifierName("SqlType"), "DateTime"),
                SqlDataTypeOption.DateTime2 => InvokeMember(IdentifierName("SqlType"), "DateTime"),
                SqlDataTypeOption.SmallDateTime => InvokeMember(IdentifierName("SqlType"), "DateTime"),
                SqlDataTypeOption.Date => InvokeMember(IdentifierName("SqlType"), "DateTime", LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                SqlDataTypeOption.Time => InvokeMember(IdentifierName("SqlType"), "DateTime"),
                SqlDataTypeOption.DateTimeOffset => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("DateTimeOffset")),
                SqlDataTypeOption.UniqueIdentifier => MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, IdentifierName("SqlType"), IdentifierName("Guid")),
                SqlDataTypeOption.VarBinary => InvokeMember(IdentifierName("SqlType"), "ByteArray", LiteralExpression(SyntaxKind.NullLiteralExpression)),
                SqlDataTypeOption.Binary => InvokeMember(IdentifierName("SqlType"), "ByteArray", LiteralExpression(SyntaxKind.NullLiteralExpression)),
                SqlDataTypeOption.Image => InvokeMember(IdentifierName("SqlType"), "ByteArray", LiteralExpression(SyntaxKind.NullLiteralExpression)),
                SqlDataTypeOption.Timestamp => InvokeMember(IdentifierName("SqlType"), "ByteArray", LiteralExpression(SyntaxKind.NullLiteralExpression)),
                SqlDataTypeOption.VarChar => InvokeMember(IdentifierName("SqlType"), "String"),
                SqlDataTypeOption.NVarChar => InvokeMember(IdentifierName("SqlType"), "String", LiteralExpression(SyntaxKind.NullLiteralExpression), LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                SqlDataTypeOption.Char => InvokeMember(IdentifierName("SqlType"), "String"),
                SqlDataTypeOption.NChar => InvokeMember(IdentifierName("SqlType"), "String", LiteralExpression(SyntaxKind.NullLiteralExpression), LiteralExpression(SyntaxKind.TrueLiteralExpression)),
                SqlDataTypeOption.Text => InvokeMember(IdentifierName("SqlType"), "String"),
                SqlDataTypeOption.NText => InvokeMember(IdentifierName("SqlType"), "String", LiteralExpression(SyntaxKind.NullLiteralExpression), LiteralExpression(SyntaxKind.TrueLiteralExpression)),
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
            if (functionCall.OverClause != null)
            {
                return this.BuildWindowFunctionCall(functionCall, context, wrapLiterals);
            }

            var functionName = functionCall.FunctionName.Value;
            if (functionCall.CallTarget != null)
            {
                if (functionCall.UniqueRowFilter == UniqueRowFilter.Distinct)
                {
                    throw new SqExpressSqlTranspilerException("DISTINCT is not supported for schema-qualified function calls.");
                }

                if (!TryGetFunctionSchemaName(functionCall.CallTarget, out var schemaName))
                {
                    throw new SqExpressSqlTranspilerException(
                        $"Unsupported function call target type: {functionCall.CallTarget.GetType().Name}.");
                }

                var customFunctionArgs = functionCall.Parameters.Select(arg =>
                {
                    if (IsStar(arg))
                    {
                        throw new SqExpressSqlTranspilerException($"Function '{functionName}' with '*' argument is not supported.");
                    }

                    return this.BuildScalarExpression(arg, context, wrapLiterals);
                }).ToList();

                return customFunctionArgs.Count == 0
                    ? Invoke("ScalarFunctionCustom", StringLiteral(schemaName), StringLiteral(functionName))
                    : Invoke("ScalarFunctionCustom", Prepend(StringLiteral(schemaName), Prepend(StringLiteral(functionName), customFunctionArgs)));
            }

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

        private static bool TryGetFunctionSchemaName(CallTarget callTarget, out string schemaName)
        {
            schemaName = string.Empty;
            switch (callTarget)
            {
                case MultiPartIdentifierCallTarget multiPartIdentifierCallTarget:
                {
                    var identifiers = multiPartIdentifierCallTarget.MultiPartIdentifier?.Identifiers;
                    if (identifiers == null || identifiers.Count < 1)
                    {
                        return false;
                    }

                    if (identifiers.Count > 1)
                    {
                        throw new SqExpressSqlTranspilerException(
                            "Database-qualified scalar function calls are not supported yet.");
                    }

                    if (string.IsNullOrWhiteSpace(identifiers[0].Value))
                    {
                        return false;
                    }

                    schemaName = identifiers[0].Value;
                    return true;
                }
                case UserDefinedTypeCallTarget userDefinedTypeCallTarget:
                {
                    var schemaObject = userDefinedTypeCallTarget.SchemaObjectName;
                    if (schemaObject?.DatabaseIdentifier != null)
                    {
                        throw new SqExpressSqlTranspilerException(
                            "Database-qualified scalar function calls are not supported yet.");
                    }

                    var schemaIdentifier = schemaObject?.SchemaIdentifier;
                    if (schemaIdentifier == null || string.IsNullOrWhiteSpace(schemaIdentifier.Value))
                    {
                        return false;
                    }

                    schemaName = schemaIdentifier.Value;
                    return true;
                }
                default:
                    return false;
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

        private SqlVariableKind InferScalarVariableKind(ScalarExpression expression, TranspileContext context)
        {
            if (expression is StringLiteral)
            {
                return SqlVariableKind.StringScalar;
            }

            if (expression is IntegerLiteral)
            {
                return SqlVariableKind.Int32Scalar;
            }

            if (expression is Microsoft.SqlServer.TransactSql.ScriptDom.NumericLiteral
                || expression is MoneyLiteral)
            {
                return SqlVariableKind.DecimalScalar;
            }

            if (expression is BinaryLiteral)
            {
                return SqlVariableKind.ByteArrayScalar;
            }

            if (expression is ParenthesisExpression parenthesisExpression)
            {
                return this.InferScalarVariableKind(parenthesisExpression.Expression, context);
            }

            if (expression is CastCall castCall && castCall.DataType is SqlDataTypeReference castDataType)
            {
                return this.MapSqlTypeToScalarVariableKind(castDataType.SqlDataTypeOption);
            }

            if (expression is ColumnReferenceExpression columnReference
                && context.TryResolveKnownColumnKind(columnReference, out var knownColumnKind))
            {
                return this.MapDescriptorKindToScalarVariableKind(knownColumnKind);
            }

            if (expression is ColumnReferenceExpression unresolvedColumnReference)
            {
                var registeredColumn = context.RegisterColumnReference(unresolvedColumnReference, DescriptorColumnKind.Int32);
                if (registeredColumn.HasValue)
                {
                    return this.MapDescriptorKindToScalarVariableKind(registeredColumn.Value.Column.Kind);
                }
            }

            if (expression is VariableReference variableReference
                && context.TryGetSqlVariableKind(variableReference.Name, out var existingVariableKind))
            {
                return existingVariableKind;
            }

            return SqlVariableKind.UnknownScalar;
        }

        private bool TryExtractVariableReference(ScalarExpression expression, out string variableName, out SqlVariableKind kindHint)
        {
            variableName = string.Empty;
            kindHint = SqlVariableKind.UnknownScalar;

            if (expression is VariableReference variableReference)
            {
                variableName = variableReference.Name;
                return true;
            }

            if (expression is ParenthesisExpression parenthesisExpression)
            {
                return this.TryExtractVariableReference(parenthesisExpression.Expression, out variableName, out kindHint);
            }

            if (expression is CastCall castCall
                && castCall.Parameter is VariableReference castVariable
                && castCall.DataType is SqlDataTypeReference castDataType)
            {
                variableName = castVariable.Name;
                kindHint = this.MapSqlTypeToScalarVariableKind(castDataType.SqlDataTypeOption);
                return true;
            }

            return false;
        }

        private SqlVariableKind InferListVariableKind(ColumnReferenceExpression inColumn, TranspileContext context)
        {
            if (context.TryResolveKnownColumnKind(inColumn, out var knownColumnKind))
            {
                if (knownColumnKind == DescriptorColumnKind.Int32)
                {
                    return SqlVariableKind.StringList;
                }

                return this.MapDescriptorKindToListVariableKind(knownColumnKind);
            }

            return SqlVariableKind.StringList;
        }

        private bool TryInferDescriptorColumnKind(ScalarExpression expression, TranspileContext context, out DescriptorColumnKind kind)
        {
            kind = DescriptorColumnKind.Int32;

            if (expression is StringLiteral)
            {
                kind = DescriptorColumnKind.NVarChar;
                return true;
            }

            if (expression is BinaryLiteral)
            {
                kind = DescriptorColumnKind.ByteArray;
                return true;
            }

            if (expression is Microsoft.SqlServer.TransactSql.ScriptDom.NumericLiteral || expression is MoneyLiteral)
            {
                kind = DescriptorColumnKind.Decimal;
                return true;
            }

            if (expression is ParenthesisExpression parenthesisExpression)
            {
                return this.TryInferDescriptorColumnKind(parenthesisExpression.Expression, context, out kind);
            }

            if (expression is CastCall castCall && castCall.DataType is SqlDataTypeReference castDataType)
            {
                kind = this.MapSqlTypeToDescriptorKind(castDataType.SqlDataTypeOption);
                return true;
            }

            if (expression is VariableReference variableReference
                && context.TryGetSqlVariableKind(variableReference.Name, out var sqlVariableKind))
            {
                if (this.TryMapVariableKindToDescriptorKind(sqlVariableKind, out kind))
                {
                    return true;
                }
            }

            if (expression is ColumnReferenceExpression columnReference
                && context.TryResolveKnownColumnKind(columnReference, out var knownColumnKind))
            {
                kind = knownColumnKind;
                return true;
            }

            return false;
        }

        private bool ShouldPreferDecimal(ScalarExpression left, ScalarExpression right, TranspileContext context)
        {
            if (left is Microsoft.SqlServer.TransactSql.ScriptDom.NumericLiteral
                || left is MoneyLiteral
                || right is Microsoft.SqlServer.TransactSql.ScriptDom.NumericLiteral
                || right is MoneyLiteral)
            {
                return true;
            }

            if (this.TryInferDescriptorColumnKind(left, context, out var leftKind) && leftKind == DescriptorColumnKind.Decimal)
            {
                return true;
            }

            if (this.TryInferDescriptorColumnKind(right, context, out var rightKind) && rightKind == DescriptorColumnKind.Decimal)
            {
                return true;
            }

            return false;
        }

        private void MarkColumnReferencesAsKind(ScalarExpression expression, DescriptorColumnKind kind, TranspileContext context)
        {
            foreach (var column in EnumerateColumnReferences(expression))
            {
                context.MarkColumnAsKind(column, kind);
            }
        }

        private static bool IsArithmeticBinary(BinaryExpressionType type)
        {
            switch (type)
            {
                case BinaryExpressionType.Add:
                case BinaryExpressionType.Subtract:
                case BinaryExpressionType.Multiply:
                case BinaryExpressionType.Divide:
                case BinaryExpressionType.Modulo:
                    return true;
                default:
                    return false;
            }
        }

        private static IReadOnlyList<ColumnReferenceExpression> EnumerateColumnReferences(ScalarExpression expression)
        {
            var result = new List<ColumnReferenceExpression>();
            CollectColumnReferences(expression, result);
            return result;
        }

        private static void CollectColumnReferences(ScalarExpression expression, List<ColumnReferenceExpression> result)
        {
            switch (expression)
            {
                case ColumnReferenceExpression columnReference when columnReference.ColumnType != ColumnType.Wildcard:
                    result.Add(columnReference);
                    return;
                case ParenthesisExpression parenthesis:
                    CollectColumnReferences(parenthesis.Expression, result);
                    return;
                case UnaryExpression unary:
                    CollectColumnReferences(unary.Expression, result);
                    return;
                case BinaryExpression binary:
                    CollectColumnReferences(binary.FirstExpression, result);
                    CollectColumnReferences(binary.SecondExpression, result);
                    return;
                case CastCall castCall:
                    CollectColumnReferences(castCall.Parameter, result);
                    return;
                case CoalesceExpression coalesce:
                    foreach (var inner in coalesce.Expressions)
                    {
                        CollectColumnReferences(inner, result);
                    }

                    return;
                case FunctionCall functionCall:
                    foreach (var parameter in functionCall.Parameters)
                    {
                        CollectColumnReferences(parameter, result);
                    }

                    return;
                default:
                    return;
            }
        }

        private SqlVariableKind MapSqlTypeToScalarVariableKind(SqlDataTypeOption dataTypeOption)
        {
            return dataTypeOption switch
            {
                SqlDataTypeOption.Bit => SqlVariableKind.BooleanScalar,
                SqlDataTypeOption.Int or SqlDataTypeOption.BigInt or SqlDataTypeOption.SmallInt or SqlDataTypeOption.TinyInt => SqlVariableKind.Int32Scalar,
                SqlDataTypeOption.Decimal or SqlDataTypeOption.Numeric or SqlDataTypeOption.Money or SqlDataTypeOption.SmallMoney => SqlVariableKind.DecimalScalar,
                SqlDataTypeOption.DateTime or SqlDataTypeOption.DateTime2 or SqlDataTypeOption.SmallDateTime or SqlDataTypeOption.Date or SqlDataTypeOption.Time => SqlVariableKind.DateTimeScalar,
                SqlDataTypeOption.DateTimeOffset => SqlVariableKind.DateTimeOffsetScalar,
                SqlDataTypeOption.UniqueIdentifier => SqlVariableKind.GuidScalar,
                SqlDataTypeOption.VarBinary or SqlDataTypeOption.Binary or SqlDataTypeOption.Image or SqlDataTypeOption.Timestamp => SqlVariableKind.ByteArrayScalar,
                SqlDataTypeOption.VarChar or SqlDataTypeOption.NVarChar or SqlDataTypeOption.Char or SqlDataTypeOption.NChar or SqlDataTypeOption.Text or SqlDataTypeOption.NText => SqlVariableKind.StringScalar,
                _ => SqlVariableKind.UnknownScalar
            };
        }

        private DescriptorColumnKind MapSqlTypeToDescriptorKind(SqlDataTypeOption dataTypeOption)
        {
            return dataTypeOption switch
            {
                SqlDataTypeOption.Bit => DescriptorColumnKind.Boolean,
                SqlDataTypeOption.Decimal or SqlDataTypeOption.Numeric or SqlDataTypeOption.Money or SqlDataTypeOption.SmallMoney => DescriptorColumnKind.Decimal,
                SqlDataTypeOption.DateTime or SqlDataTypeOption.DateTime2 or SqlDataTypeOption.SmallDateTime or SqlDataTypeOption.Date or SqlDataTypeOption.Time => DescriptorColumnKind.DateTime,
                SqlDataTypeOption.DateTimeOffset => DescriptorColumnKind.DateTimeOffset,
                SqlDataTypeOption.UniqueIdentifier => DescriptorColumnKind.Guid,
                SqlDataTypeOption.VarBinary or SqlDataTypeOption.Binary or SqlDataTypeOption.Image or SqlDataTypeOption.Timestamp => DescriptorColumnKind.ByteArray,
                SqlDataTypeOption.VarChar or SqlDataTypeOption.NVarChar or SqlDataTypeOption.Char or SqlDataTypeOption.NChar or SqlDataTypeOption.Text or SqlDataTypeOption.NText => DescriptorColumnKind.NVarChar,
                _ => DescriptorColumnKind.Int32
            };
        }

        private SqlVariableKind MapDescriptorKindToScalarVariableKind(DescriptorColumnKind kind)
        {
            return kind switch
            {
                DescriptorColumnKind.NVarChar => SqlVariableKind.StringScalar,
                DescriptorColumnKind.Boolean => SqlVariableKind.BooleanScalar,
                DescriptorColumnKind.Decimal => SqlVariableKind.DecimalScalar,
                DescriptorColumnKind.DateTime => SqlVariableKind.DateTimeScalar,
                DescriptorColumnKind.DateTimeOffset => SqlVariableKind.DateTimeOffsetScalar,
                DescriptorColumnKind.Guid => SqlVariableKind.GuidScalar,
                DescriptorColumnKind.ByteArray => SqlVariableKind.ByteArrayScalar,
                _ => SqlVariableKind.Int32Scalar
            };
        }

        private SqlVariableKind MapDescriptorKindToListVariableKind(DescriptorColumnKind kind)
        {
            return kind switch
            {
                DescriptorColumnKind.NVarChar => SqlVariableKind.StringList,
                DescriptorColumnKind.Boolean => SqlVariableKind.BooleanList,
                DescriptorColumnKind.Decimal => SqlVariableKind.DecimalList,
                DescriptorColumnKind.DateTime => SqlVariableKind.DateTimeList,
                DescriptorColumnKind.DateTimeOffset => SqlVariableKind.DateTimeOffsetList,
                DescriptorColumnKind.Guid => SqlVariableKind.GuidList,
                DescriptorColumnKind.ByteArray => SqlVariableKind.ByteArrayList,
                _ => SqlVariableKind.Int32List
            };
        }

        private bool TryMapVariableKindToDescriptorKind(SqlVariableKind variableKind, out DescriptorColumnKind kind)
        {
            kind = DescriptorColumnKind.Int32;
            switch (variableKind)
            {
                case SqlVariableKind.StringScalar:
                case SqlVariableKind.StringList:
                    kind = DescriptorColumnKind.NVarChar;
                    return true;
                case SqlVariableKind.BooleanScalar:
                case SqlVariableKind.BooleanList:
                    kind = DescriptorColumnKind.Boolean;
                    return true;
                case SqlVariableKind.DecimalScalar:
                case SqlVariableKind.DecimalList:
                    kind = DescriptorColumnKind.Decimal;
                    return true;
                case SqlVariableKind.DateTimeScalar:
                case SqlVariableKind.DateTimeList:
                    kind = DescriptorColumnKind.DateTime;
                    return true;
                case SqlVariableKind.DateTimeOffsetScalar:
                case SqlVariableKind.DateTimeOffsetList:
                    kind = DescriptorColumnKind.DateTimeOffset;
                    return true;
                case SqlVariableKind.GuidScalar:
                case SqlVariableKind.GuidList:
                    kind = DescriptorColumnKind.Guid;
                    return true;
                case SqlVariableKind.ByteArrayScalar:
                case SqlVariableKind.ByteArrayList:
                    kind = DescriptorColumnKind.ByteArray;
                    return true;
                case SqlVariableKind.Int32Scalar:
                case SqlVariableKind.Int32List:
                    kind = DescriptorColumnKind.Int32;
                    return true;
                default:
                    return false;
            }
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

        private static bool IsLiteralOnlyExpression(ScalarExpression expression)
        {
            switch (expression)
            {
                case Microsoft.SqlServer.TransactSql.ScriptDom.NullLiteral:
                case Microsoft.SqlServer.TransactSql.ScriptDom.StringLiteral:
                case Microsoft.SqlServer.TransactSql.ScriptDom.IntegerLiteral:
                case Microsoft.SqlServer.TransactSql.ScriptDom.NumericLiteral:
                case Microsoft.SqlServer.TransactSql.ScriptDom.MoneyLiteral:
                case Microsoft.SqlServer.TransactSql.ScriptDom.BinaryLiteral:
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

        private static bool IsSelectOneProjection(QuerySpecification specification)
        {
            if (specification.SelectElements.Count != 1)
            {
                return false;
            }

            if (specification.SelectElements[0] is not SelectScalarExpression scalar)
            {
                return false;
            }

            if (TryGetSelectAlias(scalar.ColumnName) != null)
            {
                return false;
            }

            return TryExtractInt32Constant(scalar.Expression, out var value) && value == 1;
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
                case ParenthesisExpression parenthesisExpression:
                    return TryExtractInt32Constant(parenthesisExpression.Expression, out value);
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

        private IReadOnlyList<SubQueryProjectedTableBinding> ExtractSubQueryProjectedTableBindings(
            QueryExpression queryExpression,
            TranspileContext context)
        {
            if (!TryUnwrapQuerySpecification(queryExpression, out var specification))
            {
                return Array.Empty<SubQueryProjectedTableBinding>();
            }

            var result = new List<SubQueryProjectedTableBinding>();
            var exprIndex = 0;
            foreach (var selectElement in specification.SelectElements)
            {
                if (selectElement is not SelectScalarExpression scalar)
                {
                    continue;
                }

                var outputName = TryGetSelectAlias(scalar.ColumnName);
                if (string.IsNullOrWhiteSpace(outputName) && scalar.Expression is ColumnReferenceExpression outputColRef)
                {
                    var outputIds = outputColRef.MultiPartIdentifier?.Identifiers;
                    outputName = outputIds != null && outputIds.Count > 0
                        ? outputIds[outputIds.Count - 1].Value
                        : null;
                }

                if (string.IsNullOrWhiteSpace(outputName))
                {
                    exprIndex++;
                    outputName = "Expr" + exprIndex.ToString(CultureInfo.InvariantCulture);
                }

                if (scalar.Expression is not ColumnReferenceExpression sourceColumnReference)
                {
                    continue;
                }

                if (!context.TryResolveColumnSource(sourceColumnReference, out var source))
                {
                    continue;
                }

                if (source.Descriptor?.Kind != DescriptorKind.Table)
                {
                    continue;
                }

                var sourceIds = sourceColumnReference.MultiPartIdentifier?.Identifiers;
                if (sourceIds == null || sourceIds.Count < 1)
                {
                    continue;
                }

                var sourceColumnName = sourceIds[sourceIds.Count - 1].Value;
                if (string.IsNullOrWhiteSpace(sourceColumnName))
                {
                    continue;
                }

                if (!source.Descriptor.TryGetColumn(sourceColumnName!, out var sourceColumn))
                {
                    continue;
                }

                result.Add(new SubQueryProjectedTableBinding(outputName!, source.VariableName, sourceColumn));
            }

            return result;
        }

        private static bool TryUnwrapQuerySpecification(QueryExpression queryExpression, out QuerySpecification specification)
        {
            specification = null!;
            try
            {
                specification = UnwrapQuerySpecification(queryExpression, "Query specification is expected.");
                return true;
            }
            catch (SqExpressSqlTranspilerException)
            {
                return false;
            }
        }

        private static ExpressionSyntax ReplaceAsStringAliasWithColumnProperty(
            ExpressionSyntax expression,
            IReadOnlyDictionary<string, string> aliasToPropertyMap)
        {
            if (aliasToPropertyMap.Count < 1)
            {
                return expression;
            }

            var targets = expression.DescendantNodesAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .Where(i =>
                    i.Expression is MemberAccessExpressionSyntax ma
                    && ma.Name is IdentifierNameSyntax name
                    && string.Equals(name.Identifier.ValueText, "As", StringComparison.Ordinal)
                    && i.ArgumentList.Arguments.Count == 1
                    && i.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax literal
                    && literal.IsKind(SyntaxKind.StringLiteralExpression)
                    && aliasToPropertyMap.ContainsKey(literal.Token.ValueText))
                .ToList();

            if (targets.Count < 1)
            {
                return expression;
            }

            return expression.ReplaceNodes(
                targets,
                (current, _) =>
                {
                    var literal = (LiteralExpressionSyntax)current.ArgumentList.Arguments[0].Expression;
                    var propertyName = aliasToPropertyMap[literal.Token.ValueText];
                    return current.WithArgumentList(
                        ArgumentList(
                            SingletonSeparatedList(
                                Argument(
                                    MemberAccessExpression(
                                        SyntaxKind.SimpleMemberAccessExpression,
                                        ThisExpression(),
                                        IdentifierName(propertyName))))));
                });
        }

        private static string? TryGetLocalDeclarationVariableName(LocalDeclarationStatementSyntax declaration)
        {
            if (declaration.Declaration.Variables.Count == 1)
            {
                return declaration.Declaration.Variables[0].Identifier.ValueText;
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

        private static ExpressionSyntax ParseBinaryLiteral(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                throw new SqExpressSqlTranspilerException($"Could not parse binary literal '{value}'.");
            }

            var hex = value.Substring(2);
            if (hex.Length % 2 != 0)
            {
                throw new SqExpressSqlTranspilerException($"Binary literal '{value}' has odd number of digits.");
            }

            var items = new List<ExpressionSyntax>(hex.Length / 2);
            for (var i = 0; i < hex.Length; i += 2)
            {
                var byteValue = Convert.ToByte(hex.Substring(i, 2), 16);
                items.Add(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(byteValue)));
            }

            return ArrayCreationExpression(ArrayType(PredefinedType(Token(SyntaxKind.ByteKeyword)))
                    .WithRankSpecifiers(
                        SingletonList(
                            ArrayRankSpecifier(
                                SingletonSeparatedList<ExpressionSyntax>(
                                    OmittedArraySizeExpression())))))
                .WithInitializer(InitializerExpression(SyntaxKind.ArrayInitializerExpression, SeparatedList(items)));
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

        private static string CreateUniqueIdentifier(string seed, ISet<string> reservedNames)
        {
            var normalized = NormalizeIdentifier(seed, "v");
            normalized = char.ToLowerInvariant(normalized[0]) + normalized.Substring(1);

            var candidate = normalized;
            var index = 0;
            while (reservedNames.Contains(candidate))
            {
                index++;
                candidate = normalized + index.ToString(CultureInfo.InvariantCulture);
            }

            reservedNames.Add(candidate);
            return candidate;
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

        private sealed class SqQueryBuilderCallQualifier : CSharpSyntaxRewriter
        {
            private readonly HashSet<string> _excludedNames;
            private static readonly ExpressionSyntax SqQueryBuilderExpression = IdentifierName("SqQueryBuilder");

            public SqQueryBuilderCallQualifier(HashSet<string> excludedNames)
            {
                this._excludedNames = excludedNames;
            }

            public override SyntaxNode? VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                var visited = (InvocationExpressionSyntax)base.VisitInvocationExpression(node)!;
                if (visited.Expression is IdentifierNameSyntax identifierName
                    && !this._excludedNames.Contains(identifierName.Identifier.ValueText))
                {
                    return visited.WithExpression(
                        MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SqQueryBuilderExpression,
                            IdentifierName(identifierName.Identifier.ValueText)));
                }

                return visited;
            }
        }

        private sealed class TranspileContext
        {
            private readonly SharedDescriptorState _sharedState;
            private readonly TranspileContext? _parent;
            private readonly List<TableSource> _sources = new();
            private readonly List<LocalDeclarationStatementSyntax> _sourceDeclarations = new();
            private readonly Dictionary<NamedTableReference, TableSource> _namedSourceMap = new(ReferenceEqualityComparer<NamedTableReference>.Instance);
            private readonly Dictionary<QueryDerivedTable, TableSource> _derivedSourceMap = new(ReferenceEqualityComparer<QueryDerivedTable>.Instance);
            private readonly Dictionary<InlineDerivedTable, TableSource> _inlineDerivedSourceMap = new(ReferenceEqualityComparer<InlineDerivedTable>.Instance);
            private readonly Dictionary<SchemaObjectFunctionTableReference, TableSource> _schemaFunctionSourceMap = new(ReferenceEqualityComparer<SchemaObjectFunctionTableReference>.Instance);
            private readonly Dictionary<BuiltInFunctionTableReference, TableSource> _builtInFunctionSourceMap = new(ReferenceEqualityComparer<BuiltInFunctionTableReference>.Instance);
            private readonly Dictionary<GlobalFunctionTableReference, TableSource> _globalFunctionSourceMap = new(ReferenceEqualityComparer<GlobalFunctionTableReference>.Instance);
            private readonly Dictionary<string, SqlVariable> _sqlVariableMap = new(StringComparer.OrdinalIgnoreCase);
            private readonly List<SqlVariable> _sqlVariables = new();
            private readonly Dictionary<string, TableSource> _sourceByAlias = new(StringComparer.OrdinalIgnoreCase);
            private readonly Dictionary<string, TableSource?> _sourceByObjectName = new(StringComparer.OrdinalIgnoreCase);
            private readonly HashSet<string> _variableNames;
            private readonly string _tableDescriptorClassPrefix;
            private readonly string _tableDescriptorClassSuffix;
            private readonly string? _defaultSchemaName;
            private static readonly ExpressionSyntax _guidDefaultInitializer = ParseExpression("default(global::System.Guid)");
            private static readonly ExpressionSyntax _dateTimeDefaultInitializer = ParseExpression("default(global::System.DateTime)");
            private static readonly ExpressionSyntax _dateTimeOffsetDefaultInitializer = ParseExpression("default(global::System.DateTimeOffset)");
            private static readonly ExpressionSyntax _byteArrayEmptyInitializer = ParseExpression("global::System.Array.Empty<byte>()");
            private static readonly ExpressionSyntax _stringListInitializer = ParseExpression("new[] { Literal(\"\") }");
            private static readonly ExpressionSyntax _intListInitializer = ParseExpression("new[] { Literal(0) }");
            private static readonly ExpressionSyntax _decimalListInitializer = ParseExpression("new[] { Literal(0m) }");
            private static readonly ExpressionSyntax _boolListInitializer = ParseExpression("new[] { Literal(false) }");
            private static readonly ExpressionSyntax _guidListInitializer = ParseExpression("new[] { Literal(default(global::System.Guid)) }");
            private static readonly ExpressionSyntax _dateTimeListInitializer = ParseExpression("new[] { Literal(default(global::System.DateTime)) }");
            private static readonly ExpressionSyntax _dateTimeOffsetListInitializer = ParseExpression("new[] { Literal(default(global::System.DateTimeOffset)) }");
            private static readonly ExpressionSyntax _byteArrayListInitializer = ParseExpression("new[] { Literal(global::System.Array.Empty<byte>()) }");

            public TranspileContext(SqExpressSqlTranspilerOptions options)
                : this(
                    new SharedDescriptorState(),
                    new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    parent: null,
                    tableDescriptorClassPrefix: options.TableDescriptorClassPrefix ?? string.Empty,
                    tableDescriptorClassSuffix: options.TableDescriptorClassSuffix ?? string.Empty,
                    defaultSchemaName: options.EffectiveDefaultSchemaName)
            {
            }

            private TranspileContext(
                SharedDescriptorState sharedState,
                HashSet<string> variableNames,
                TranspileContext? parent,
                string tableDescriptorClassPrefix,
                string tableDescriptorClassSuffix,
                string? defaultSchemaName)
            {
                this._sharedState = sharedState;
                this._variableNames = variableNames;
                this._parent = parent;
                this._tableDescriptorClassPrefix = tableDescriptorClassPrefix;
                this._tableDescriptorClassSuffix = tableDescriptorClassSuffix;
                this._defaultSchemaName = defaultSchemaName;
            }

            public TranspileContext CreateChild(bool shareVariableNames = false, bool inheritSourceResolution = false)
                => new TranspileContext(
                    this._sharedState,
                    shareVariableNames
                        ? this._variableNames
                        : new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                    parent: inheritSourceResolution ? this : null,
                    tableDescriptorClassPrefix: this._tableDescriptorClassPrefix,
                    tableDescriptorClassSuffix: this._tableDescriptorClassSuffix,
                    defaultSchemaName: this._defaultSchemaName);

            public IReadOnlyList<TableDescriptor> Descriptors => this._sharedState.Descriptors;

            public IReadOnlyList<TableSource> Sources => this._sources;

            public IReadOnlyList<LocalDeclarationStatementSyntax> SourceDeclarations => this._sourceDeclarations;

            public IReadOnlyList<LocalDeclarationStatementSyntax> ParameterDeclarations
                => this._sqlVariables
                    .Select(this.CreateSqlVariableDeclaration)
                    .ToList();

            public void AbsorbSourceDeclarations(TranspileContext context)
            {
                this._sourceDeclarations.AddRange(context._sourceDeclarations);
            }

            public void AbsorbSqlVariables(TranspileContext context)
            {
                foreach (var sqlVariable in context._sqlVariables)
                {
                    if (this._sqlVariableMap.TryGetValue(sqlVariable.SqlName, out var existing))
                    {
                        existing.Kind = MergeSqlVariableKinds(existing.Kind, sqlVariable.Kind);
                        continue;
                    }

                    var copied = new SqlVariable(sqlVariable.SqlName, sqlVariable.VariableName, sqlVariable.Kind);
                    this._sqlVariableMap[copied.SqlName] = copied;
                    this._sqlVariables.Add(copied);
                    this._variableNames.Add(copied.VariableName);
                }
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
                    var schemaName = tableReference.SchemaObject.SchemaIdentifier?.Value;
                    var databaseName = tableReference.SchemaObject.DatabaseIdentifier?.Value;

                    if (schemaName == null && databaseName == null)
                    {
                        descriptor = this.GetOrAddUnqualifiedTableDescriptor(objectName!, this._defaultSchemaName);
                    }
                    else
                    {
                        descriptor = this.GetOrAddQualifiedTableDescriptor(databaseName, schemaName, objectName!);
                    }
                }

                var source = this.RegisterSource(descriptor, alias ?? objectName!, alias, objectName!);
                this._namedSourceMap[tableReference] = source;
                return source;
            }

            private static string CreateTableDescriptorKey(string? databaseName, string? schemaName, string objectName)
                => (databaseName ?? string.Empty)
                   + "|"
                   + (schemaName ?? string.Empty)
                   + "|"
                   + objectName;

            private TableDescriptor GetOrAddUnqualifiedTableDescriptor(string objectName, string? defaultSchemaName)
            {
                var tableDescriptors = this.GetTableDescriptorsByObjectName(objectName);
                if (!string.IsNullOrWhiteSpace(defaultSchemaName))
                {
                    var matchingDefaultSchema = tableDescriptors
                        .Where(i =>
                            i.DatabaseName == null
                            && string.Equals(i.SchemaName, defaultSchemaName, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (matchingDefaultSchema.Count == 1)
                    {
                        return matchingDefaultSchema[0];
                    }

                    if (matchingDefaultSchema.Count > 1)
                    {
                        throw new SqExpressSqlTranspilerException(
                            $"Ambiguous unqualified table reference '{objectName}'. " +
                            $"Multiple descriptors found for default schema '{defaultSchemaName}'.");
                    }

                    return this.CreateAndRegisterTableDescriptor(objectName, schemaName: defaultSchemaName, databaseName: null);
                }

                if (tableDescriptors.Count > 1)
                {
                    throw new SqExpressSqlTranspilerException(
                        $"Ambiguous unqualified table reference '{objectName}'. " +
                        "Multiple schema-qualified descriptors are already present.");
                }

                if (tableDescriptors.Count == 1)
                {
                    return tableDescriptors[0];
                }

                return this.CreateAndRegisterTableDescriptor(objectName, schemaName: null, databaseName: null);
            }

            private TableDescriptor GetOrAddQualifiedTableDescriptor(string? databaseName, string? schemaName, string objectName)
            {
                var tableDescriptorKey = CreateTableDescriptorKey(databaseName, schemaName, objectName);
                if (this._sharedState.TableDescriptors.TryGetValue(tableDescriptorKey, out var existingQualified))
                {
                    return existingQualified;
                }

                var tableDescriptors = this.GetTableDescriptorsByObjectName(objectName);
                var unqualified = tableDescriptors
                    .Where(i => i.SchemaName == null && i.DatabaseName == null)
                    .ToList();

                if (unqualified.Count == 1 && tableDescriptors.Count == 1)
                {
                    var descriptor = unqualified[0];
                    var oldKey = CreateTableDescriptorKey(descriptor.DatabaseName, descriptor.SchemaName, descriptor.ObjectName);
                    descriptor.SetQualification(schemaName, databaseName);
                    this._sharedState.TableDescriptors.Remove(oldKey);
                    this._sharedState.TableDescriptors[tableDescriptorKey] = descriptor;
                    return descriptor;
                }

                return this.CreateAndRegisterTableDescriptor(objectName, schemaName, databaseName);
            }

            private TableDescriptor CreateAndRegisterTableDescriptor(string objectName, string? schemaName, string? databaseName)
            {
                var descriptor = new TableDescriptor(
                    className: this.CreateTableClassName(objectName),
                    kind: DescriptorKind.Table,
                    objectName: objectName,
                    schemaName: schemaName,
                    databaseName: databaseName,
                    queryExpression: null);

                var tableDescriptorKey = CreateTableDescriptorKey(databaseName, schemaName, objectName);
                this._sharedState.TableDescriptors[tableDescriptorKey] = descriptor;
                if (!this._sharedState.TableDescriptorsByObjectName.TryGetValue(objectName, out var byObjectName))
                {
                    byObjectName = new List<TableDescriptor>();
                    this._sharedState.TableDescriptorsByObjectName[objectName] = byObjectName;
                }

                byObjectName.Add(descriptor);
                this._sharedState.Descriptors.Add(descriptor);
                return descriptor;
            }

            private IReadOnlyList<TableDescriptor> GetTableDescriptorsByObjectName(string objectName)
            {
                if (this._sharedState.TableDescriptorsByObjectName.TryGetValue(objectName, out var byObjectName))
                {
                    return byObjectName;
                }

                return Array.Empty<TableDescriptor>();
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

            public TableSource GetOrAddInlineDerivedSource(InlineDerivedTable inlineDerivedTable, SqExpressSqlTranspiler transpiler)
            {
                if (this._inlineDerivedSourceMap.TryGetValue(inlineDerivedTable, out var existing))
                {
                    return existing;
                }

                var alias = inlineDerivedTable.Alias?.Value;
                if (string.IsNullOrWhiteSpace(alias))
                {
                    throw new SqExpressSqlTranspilerException("VALUES derived table alias cannot be empty.");
                }

                var sourceExpression = transpiler.BuildInlineDerivedTableSourceExpression(inlineDerivedTable, this);
                var source = this.RegisterDynamicSource(
                    sourceExpression,
                    alias!,
                    alias,
                    alias!);

                this._inlineDerivedSourceMap[inlineDerivedTable] = source;
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

            public void AddSourceAlias(string alias, TableSource source)
            {
                if (string.IsNullOrWhiteSpace(alias))
                {
                    throw new SqExpressSqlTranspilerException("Source alias cannot be empty.");
                }

                this._sourceByAlias[alias] = source;
            }

            public SqlVariable RegisterSqlVariable(string sqlName, SqlVariableKind kindHint)
            {
                if (string.IsNullOrWhiteSpace(sqlName))
                {
                    throw new SqExpressSqlTranspilerException("SQL variable name cannot be empty.");
                }

                var normalizedSqlName = sqlName.StartsWith("@", StringComparison.Ordinal)
                    ? sqlName
                    : "@" + sqlName;

                if (this._sqlVariableMap.TryGetValue(normalizedSqlName, out var existing))
                {
                    existing.Kind = MergeSqlVariableKinds(existing.Kind, kindHint);
                    return existing;
                }

                var variableNameSeed = NormalizeSqlVariableName(normalizedSqlName);
                var variable = new SqlVariable(
                    normalizedSqlName,
                    this.CreateVariableName(variableNameSeed),
                    MergeSqlVariableKinds(SqlVariableKind.UnknownScalar, kindHint));

                this._sqlVariableMap[normalizedSqlName] = variable;
                this._sqlVariables.Add(variable);
                return variable;
            }

            public bool TryGetSqlVariableKind(string sqlName, out SqlVariableKind kind)
            {
                kind = SqlVariableKind.UnknownScalar;
                if (string.IsNullOrWhiteSpace(sqlName))
                {
                    return false;
                }

                var normalizedSqlName = sqlName.StartsWith("@", StringComparison.Ordinal)
                    ? sqlName
                    : "@" + sqlName;
                if (!this._sqlVariableMap.TryGetValue(normalizedSqlName, out var variable))
                {
                    return false;
                }

                kind = variable.Kind;
                return true;
            }

            public bool TryResolveKnownColumnKind(ColumnReferenceExpression columnReference, out DescriptorColumnKind kind)
            {
                kind = DescriptorColumnKind.Int32;
                if (!this.TryResolveColumnDescriptor(columnReference, out var descriptor, out var columnName))
                {
                    return false;
                }

                if (!descriptor.TryGetColumn(columnName, out var column))
                {
                    return false;
                }

                kind = column.Kind;
                return true;
            }

            public void MarkColumnAsKind(ColumnReferenceExpression columnReference, DescriptorColumnKind kind)
            {
                this.RegisterColumnReference(columnReference, kind);
            }

            public void MarkColumnNullable(ColumnReferenceExpression columnReference)
            {
                var registered = this.RegisterColumnReference(columnReference, DescriptorColumnKind.Int32);
                if (registered.HasValue)
                {
                    registered.Value.Column.IsNullable = true;
                }
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

            private LocalDeclarationStatementSyntax CreateSqlVariableDeclaration(SqlVariable sqlVariable)
            {
                var initializer = GetSqlVariableInitializer(sqlVariable.Kind);

                return LocalDeclarationStatement(
                    VariableDeclaration(IdentifierName("var"))
                        .AddVariables(
                            VariableDeclarator(Identifier(sqlVariable.VariableName))
                                .WithInitializer(EqualsValueClause(initializer))));
            }

            private static ExpressionSyntax GetSqlVariableInitializer(SqlVariableKind kind)
            {
                return IsListVariableKind(kind)
                    ? GetListVariableInitializer(kind)
                    : GetScalarVariableInitializer(kind);
            }

            private static ExpressionSyntax GetScalarVariableInitializer(SqlVariableKind kind)
            {
                return kind switch
                {
                    SqlVariableKind.UnknownScalar => StringLiteral(string.Empty),
                    SqlVariableKind.StringScalar => StringLiteral(string.Empty),
                    SqlVariableKind.Int32Scalar => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)),
                    SqlVariableKind.DecimalScalar => LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0m)),
                    SqlVariableKind.BooleanScalar => LiteralExpression(SyntaxKind.FalseLiteralExpression),
                    SqlVariableKind.GuidScalar => _guidDefaultInitializer,
                    SqlVariableKind.DateTimeScalar => _dateTimeDefaultInitializer,
                    SqlVariableKind.DateTimeOffsetScalar => _dateTimeOffsetDefaultInitializer,
                    SqlVariableKind.ByteArrayScalar => _byteArrayEmptyInitializer,
                    _ => StringLiteral(string.Empty)
                };
            }

            private static ExpressionSyntax GetListVariableInitializer(SqlVariableKind kind)
            {
                return kind switch
                {
                    SqlVariableKind.StringList => _stringListInitializer,
                    SqlVariableKind.Int32List => _intListInitializer,
                    SqlVariableKind.DecimalList => _decimalListInitializer,
                    SqlVariableKind.BooleanList => _boolListInitializer,
                    SqlVariableKind.GuidList => _guidListInitializer,
                    SqlVariableKind.DateTimeList => _dateTimeListInitializer,
                    SqlVariableKind.DateTimeOffsetList => _dateTimeOffsetListInitializer,
                    SqlVariableKind.ByteArrayList => _byteArrayListInitializer,
                    _ => StringLiteral(string.Empty)
                };
            }

            private static SqlVariableKind MergeSqlVariableKinds(SqlVariableKind existing, SqlVariableKind hint)
            {
                if (hint == SqlVariableKind.UnknownScalar)
                {
                    return existing;
                }

                if (existing == SqlVariableKind.UnknownScalar)
                {
                    return hint;
                }

                var existingIsList = IsListVariableKind(existing);
                var hintIsList = IsListVariableKind(hint);
                if (existingIsList != hintIsList)
                {
                    throw new SqExpressSqlTranspilerException("The same SQL variable cannot be used both as scalar and list.");
                }

                if (existing == hint)
                {
                    return existing;
                }

                if (existing is SqlVariableKind.Int32Scalar && hint is SqlVariableKind.DecimalScalar
                    || existing is SqlVariableKind.DecimalScalar && hint is SqlVariableKind.Int32Scalar)
                {
                    return SqlVariableKind.DecimalScalar;
                }

                if (existing is SqlVariableKind.Int32List && hint is SqlVariableKind.DecimalList
                    || existing is SqlVariableKind.DecimalList && hint is SqlVariableKind.Int32List)
                {
                    return SqlVariableKind.DecimalList;
                }

                return existing;
            }

            private static bool IsListVariableKind(SqlVariableKind kind)
                => kind is SqlVariableKind.StringList
                    or SqlVariableKind.Int32List
                    or SqlVariableKind.DecimalList
                    or SqlVariableKind.BooleanList
                    or SqlVariableKind.DateTimeList
                    or SqlVariableKind.DateTimeOffsetList
                    or SqlVariableKind.GuidList
                    or SqlVariableKind.ByteArrayList;

            private static string NormalizeSqlVariableName(string sqlVariableName)
            {
                var withoutPrefix = sqlVariableName.TrimStart('@');
                return string.IsNullOrWhiteSpace(withoutPrefix)
                    ? "p"
                    : withoutPrefix;
            }

            private bool TryResolveColumnDescriptor(
                ColumnReferenceExpression columnReference,
                out TableDescriptor descriptor,
                out string columnName)
            {
                descriptor = null!;
                columnName = string.Empty;

                if (columnReference.ColumnType == ColumnType.Wildcard)
                {
                    return false;
                }

                var identifiers = columnReference.MultiPartIdentifier?.Identifiers;
                if (identifiers == null || identifiers.Count < 1)
                {
                    return false;
                }

                var resolvedColumnName = identifiers[identifiers.Count - 1].Value;
                if (string.IsNullOrWhiteSpace(resolvedColumnName))
                {
                    return false;
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

                if (source?.Descriptor == null)
                {
                    return false;
                }

                descriptor = source.Descriptor;
                columnName = resolvedColumnName!;
                return true;
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

            private string CreateTableClassName(string objectName)
            {
                var source = this._tableDescriptorClassPrefix + objectName + this._tableDescriptorClassSuffix;
                return this.CreateClassName(source, "GeneratedTable");
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

            public Dictionary<string, TableDescriptor> TableDescriptors { get; } = new(StringComparer.OrdinalIgnoreCase);

            public Dictionary<string, List<TableDescriptor>> TableDescriptorsByObjectName { get; } = new(StringComparer.OrdinalIgnoreCase);

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

        private readonly struct SubQueryProjectedTableBinding
        {
            public SubQueryProjectedTableBinding(string outputColumnSqlName, string sourceVariableName, TableDescriptorColumn sourceColumn)
            {
                this.OutputColumnSqlName = outputColumnSqlName;
                this.SourceVariableName = sourceVariableName;
                this.SourceColumn = sourceColumn;
            }

            public string OutputColumnSqlName { get; }

            public string SourceVariableName { get; }

            public TableDescriptorColumn SourceColumn { get; }
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

        private sealed class SqlVariable
        {
            public SqlVariable(string sqlName, string variableName, SqlVariableKind kind)
            {
                this.SqlName = sqlName;
                this.VariableName = variableName;
                this.Kind = kind;
            }

            public string SqlName { get; }

            public string VariableName { get; }

            public SqlVariableKind Kind { get; set; }
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

            public string? SchemaName { get; private set; }

            public string? DatabaseName { get; private set; }

            public QueryExpression? QueryExpression { get; }

            public IReadOnlyList<TableDescriptorColumn> Columns => this._columns;

            public bool TryGetColumn(string sqlName, out TableDescriptorColumn column)
                => this._columnMap.TryGetValue(sqlName, out column!);

            public void SetQualification(string? schemaName, string? databaseName)
            {
                if (this.SchemaName == null && this.DatabaseName == null)
                {
                    this.SchemaName = schemaName;
                    this.DatabaseName = databaseName;
                    return;
                }

                if (!string.Equals(this.SchemaName, schemaName, StringComparison.OrdinalIgnoreCase)
                    || !string.Equals(this.DatabaseName, databaseName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new SqExpressSqlTranspilerException(
                        $"Conflicting schema/database qualification for table '{this.ObjectName}'.");
                }
            }

            public TableDescriptorColumn GetOrAddColumn(string sqlName, DescriptorColumnKind kind)
            {
                var normalizedHint = ApplyNamingHeuristics(sqlName, kind, out var hintedStringLength);
                if (this._columnMap.TryGetValue(sqlName, out var existing))
                {
                    existing.Kind = MergeDescriptorColumnKinds(existing.Kind, normalizedHint);
                    if (existing.Kind == DescriptorColumnKind.NVarChar)
                    {
                        existing.StringLength = MergeStringLength(existing.StringLength, hintedStringLength);
                    }
                    else
                    {
                        existing.StringLength = null;
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

                var column = new TableDescriptorColumn(
                    sqlName,
                    propertyName,
                    normalizedHint,
                    isNullable: false,
                    stringLength: normalizedHint == DescriptorColumnKind.NVarChar ? hintedStringLength : null);
                this._columnMap[sqlName] = column;
                this._columns.Add(column);
                return column;
            }

            private static DescriptorColumnKind ApplyNamingHeuristics(string sqlName, DescriptorColumnKind hint, out int? stringLength)
            {
                stringLength = null;
                if (hint != DescriptorColumnKind.Int32)
                {
                    return hint;
                }

                var normalized = new string(
                    sqlName
                        .Where(ch => ch != '_' && ch != '-' && !char.IsWhiteSpace(ch))
                        .ToArray())
                    .ToUpperInvariant();

                if (normalized.StartsWith("IS", StringComparison.Ordinal)
                    || normalized.StartsWith("HAS", StringComparison.Ordinal)
                    || normalized.StartsWith("CAN", StringComparison.Ordinal)
                    || normalized.EndsWith("FLAG", StringComparison.Ordinal)
                    || normalized.StartsWith("ENABLE", StringComparison.Ordinal)
                    || normalized.StartsWith("DISABLE", StringComparison.Ordinal))
                {
                    return DescriptorColumnKind.Boolean;
                }

                if (normalized.EndsWith("NAME", StringComparison.Ordinal)
                    || normalized.EndsWith("DESCRIPTION", StringComparison.Ordinal)
                    || normalized.EndsWith("TITLE", StringComparison.Ordinal)
                    || normalized.EndsWith("COMMENT", StringComparison.Ordinal)
                    || normalized.EndsWith("NOTE", StringComparison.Ordinal)
                    || normalized.EndsWith("TEXT", StringComparison.Ordinal))
                {
                    stringLength = 255;
                    return DescriptorColumnKind.NVarChar;
                }

                if (normalized.EndsWith("DATE", StringComparison.Ordinal)
                    || normalized.EndsWith("TIME", StringComparison.Ordinal)
                    || normalized.EndsWith("AT", StringComparison.Ordinal)
                    || normalized.IndexOf("UTC", StringComparison.Ordinal) >= 0
                    || normalized.IndexOf("TIMESTAMP", StringComparison.Ordinal) >= 0
                    || normalized.EndsWith("ON", StringComparison.Ordinal))
                {
                    return DescriptorColumnKind.DateTime;
                }

                if (normalized.EndsWith("GUID", StringComparison.Ordinal)
                    || normalized.EndsWith("UUID", StringComparison.Ordinal)
                    || normalized.EndsWith("UID", StringComparison.Ordinal))
                {
                    return DescriptorColumnKind.Guid;
                }

                if (normalized.EndsWith("AMOUNT", StringComparison.Ordinal)
                    || normalized.EndsWith("PRICE", StringComparison.Ordinal)
                    || normalized.EndsWith("COST", StringComparison.Ordinal)
                    || normalized.EndsWith("RATE", StringComparison.Ordinal)
                    || normalized.EndsWith("PERCENT", StringComparison.Ordinal)
                    || normalized.EndsWith("BALANCE", StringComparison.Ordinal))
                {
                    return DescriptorColumnKind.Decimal;
                }

                if (normalized.EndsWith("COUNT", StringComparison.Ordinal)
                    || normalized.EndsWith("QTY", StringComparison.Ordinal)
                    || normalized.EndsWith("QUANTITY", StringComparison.Ordinal)
                    || normalized.EndsWith("NUMBER", StringComparison.Ordinal)
                    || normalized.EndsWith("INDEX", StringComparison.Ordinal)
                    || normalized.EndsWith("ORDER", StringComparison.Ordinal)
                    || normalized.EndsWith("ID", StringComparison.Ordinal))
                {
                    return DescriptorColumnKind.Int32;
                }

                return DescriptorColumnKind.Int32;
            }

            private static int? MergeStringLength(int? existing, int? hint)
            {
                if (existing.HasValue && hint.HasValue)
                {
                    return Math.Max(existing.Value, hint.Value);
                }

                return existing ?? hint;
            }

            private static DescriptorColumnKind MergeDescriptorColumnKinds(DescriptorColumnKind existing, DescriptorColumnKind hint)
            {
                if (existing == hint)
                {
                    return existing;
                }

                if (existing == DescriptorColumnKind.Int32)
                {
                    return hint;
                }

                if (hint == DescriptorColumnKind.Int32)
                {
                    return existing;
                }

                return DescriptorColumnKind.NVarChar;
            }
        }

        private sealed class TableDescriptorColumn
        {
            public TableDescriptorColumn(string sqlName, string propertyName, DescriptorColumnKind kind, bool isNullable, int? stringLength)
            {
                this.SqlName = sqlName;
                this.PropertyName = propertyName;
                this.Kind = kind;
                this.IsNullable = isNullable;
                this.StringLength = stringLength;
            }

            public string SqlName { get; }

            public string PropertyName { get; }

            public DescriptorColumnKind Kind { get; set; }

            public bool IsNullable { get; set; }

            public int? StringLength { get; set; }
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
            NVarChar,
            Boolean,
            Decimal,
            DateTime,
            DateTimeOffset,
            Guid,
            ByteArray
        }

        private enum SqlVariableKind
        {
            UnknownScalar,
            StringScalar,
            Int32Scalar,
            DecimalScalar,
            BooleanScalar,
            DateTimeScalar,
            DateTimeOffsetScalar,
            GuidScalar,
            ByteArrayScalar,
            StringList,
            Int32List,
            DecimalList,
            BooleanList,
            DateTimeList,
            DateTimeOffsetList,
            GuidList,
            ByteArrayList
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
