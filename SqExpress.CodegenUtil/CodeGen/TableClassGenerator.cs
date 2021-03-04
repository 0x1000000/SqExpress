using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGenUtil.Model;
using static SqExpress.CodeGenUtil.CodeGen.SyntaxHelpers;
namespace SqExpress.CodeGenUtil.CodeGen
{
    internal class TableClassGenerator
    {
        public static CompilationUnitSyntax Generate(TableModel table, IReadOnlyDictionary<TableRef, TableModel> allTables, string defaultNamespace)
        {
            return SyntaxFactory.CompilationUnit()
                .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameof(SqExpress))),
                    SyntaxFactory.UsingDirective(SyntaxFactory.ParseName($"{nameof(SqExpress)}.{nameof(SqExpress.Syntax)}.{nameof(SqExpress.Syntax.Type)}")))
                .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(defaultNamespace))
                    .AddMembers(GenerateClass(table, allTables)))
                .NormalizeWhitespace();
        }

        private static ClassDeclarationSyntax GenerateClass(TableModel table, IReadOnlyDictionary<TableRef, TableModel> allTables)
        {
            return SyntaxFactory.ClassDeclaration(table.Name)
                .WithModifiers(Modifiers(SyntaxKind.PublicKeyword))
                .WithBaseList(SyntaxFactory.BaseList()
                    .AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseName(nameof(TableBase)))))
                .AddMembers(ConcatClassMembers(GenerateEmptyConstructor(table), GenerateMainConstructor(table, allTables), GenerateColumnProperties(table)));
        }

        private static ConstructorDeclarationSyntax GenerateEmptyConstructor(TableModel table)
        {
            return SyntaxFactory.ConstructorDeclaration(table.Name)
                .WithModifiers(Modifiers(SyntaxKind.PublicKeyword))
                .WithInitializer(SyntaxFactory.ConstructorInitializer(
                    SyntaxKind.ThisConstructorInitializer, 
                    argumentList: ArgumentList(("alias", SyntaxFactory.ParseName($"{nameof(SqExpress)}.{nameof(Alias)}").MemberAccess(nameof(Alias.Auto))))))
                .WithBody(SyntaxFactory.Block());
        }

        private static ConstructorDeclarationSyntax GenerateMainConstructor(TableModel table, IReadOnlyDictionary<TableRef, TableModel> allTables)
        {
            return SyntaxFactory.ConstructorDeclaration(table.Name)
                .AddParameterListParameters(FuncParameter("alias", nameof(Alias)))
                .WithModifiers(Modifiers(SyntaxKind.PublicKeyword))
                .WithInitializer(SyntaxFactory.ConstructorInitializer(
                    SyntaxKind.BaseConstructorInitializer,
                    argumentList: ArgumentList(
                        ("schema",LiteralExpr(table.DbName.Schema)),
                        ("name", LiteralExpr(table.DbName.Name)),
                        ("alias", SyntaxFactory.IdentifierName("alias")))))
                .WithBody(SyntaxFactory.Block(GenerateConstructorAssignments(table, allTables)));
        }

        private static IEnumerable<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax> GenerateConstructorAssignments(TableModel table, IReadOnlyDictionary<TableRef, TableModel> allTables)
        {
            var result = new List<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax>(table.Columns.Count + table.Indexes.Count);

            foreach (var tableColumn in table.Columns)
            {
                var statement = SyntaxFactory.ExpressionStatement(AssignmentThis(tableColumn.Name, tableColumn.ColumnType.Accept(ColumnFactoryGenerator.Instance, new ColumnContext(tableColumn, allTables))));

                result.Add(statement);
            }

            foreach (var tableIndex in table.Indexes)
            {
                Dictionary<ColumnRef, ColumnModel> allTableColumns = table.Columns.ToDictionary(i => i.DbName);

                result.Add(GenerateIndexFactory(tableIndex, allTableColumns));
            }

            return result;
        }

        private static PropertyDeclarationSyntax[] GenerateColumnProperties(TableModel table)
        {
            var result = new List<PropertyDeclarationSyntax>(table.Columns.Count);

            foreach (var tableColumn in table.Columns)
            {
                var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                        tableColumn.ColumnType.Accept(ColumnTypeGenerator.Instance, null),
                        tableColumn.Name)
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                        );

                result.Add(propertyDeclaration);
            }

            return result.ToArray();
        }

        private static Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax GenerateIndexFactory(IndexModel tableIndex, IReadOnlyDictionary<ColumnRef, ColumnModel> allTableColumns)
        {
            string name;
            if (tableIndex.IsUnique)
            {
                name = tableIndex.IsClustered
                    ? ColumnFactoryGenerator.Instance.NameOfAddUniqueClusteredIndex
                    : ColumnFactoryGenerator.Instance.NameOfAddUniqueIndex;
            }
            else
            {
                name = tableIndex.IsClustered
                    ? ColumnFactoryGenerator.Instance.NameOfAddClusteredIndex
                    : ColumnFactoryGenerator.Instance.NameOfAddIndex;
            }

            var colEs = tableIndex.Columns.Select(c =>
                {

                    if (!allTableColumns.TryGetValue(c.DbName, out var tableColumn))
                    {
                        throw new SqExpressCodeGenException($"Could not find column model for: \"{c.DbName}\"");
                    }

                    return c.IsDescending
                        ? (ExpressionSyntax) MemberAccess(nameof(IndexMetaColumn), nameof(IndexMetaColumn.Desc))
                            .Invoke(MemberAccessThis(tableColumn.Name))
                        : MemberAccessThis(tableColumn.Name);
                })
                .ToArray();


            return SyntaxFactory.ExpressionStatement(InvokeThis(name, colEs));
        }

        private static MemberDeclarationSyntax[] ConcatClassMembers(ConstructorDeclarationSyntax empty, ConstructorDeclarationSyntax main, IReadOnlyList<PropertyDeclarationSyntax> properties)
        {
            List<MemberDeclarationSyntax> result = new List<MemberDeclarationSyntax>(2 + properties.Count)
            {
                empty, main
            };

            result.AddRange(properties);

            return result.ToArray();
        }

    }
}