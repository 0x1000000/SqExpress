using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.DbMetadata.Internal.Model;
using static SqExpress.CodeGenUtil.CodeGen.SyntaxHelpers;
namespace SqExpress.CodeGenUtil.CodeGen;

internal class TableClassGenerator
{
    private readonly IReadOnlyDictionary<TableRef, TableModel> _allTables;

    private readonly string _defaultNamespace;

    private readonly IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> _existingCode;

    public TableClassGenerator(IReadOnlyDictionary<TableRef, TableModel> allTables, string defaultNamespace, IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> existingCode)
    {
        this._allTables = allTables;
        this._defaultNamespace = defaultNamespace;
        this._existingCode = existingCode;
    }

    public CompilationUnitSyntax Generate(TableModel table, out bool existing)
    {
        existing = false;
        if (this._existingCode.TryGetValue(table.DbName, out var existingTable))
        {
            existing = true;
            return this.ModifyClass(table, existingTable);
        }

        return SyntaxFactory.CompilationUnit()
            .AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName(nameof(SqExpress))),
                SyntaxFactory.UsingDirective(SyntaxFactory.ParseName($"{nameof(SqExpress)}.{nameof(SqExpress.Syntax)}.{nameof(SqExpress.Syntax.Type)}")))
            .AddMembers(SyntaxFactory.NamespaceDeclaration(SyntaxFactory.ParseName(this._defaultNamespace))
                .AddMembers(GenerateClass(table)))
            .NormalizeWhitespace();
    }

    private ClassDeclarationSyntax GenerateClass(TableModel table)
    {
        return SyntaxFactory.ClassDeclaration(table.Name)
            .WithModifiers(Modifiers(SyntaxKind.PublicKeyword))
            .WithBaseList(SyntaxFactory.BaseList()
                .AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseName(nameof(TableBase)))))
            .AddMembers(ConcatClassMembers(GenerateEmptyConstructor(table), GenerateMainConstructor(table), GenerateColumnProperties(table)));
    }

    private CompilationUnitSyntax ModifyClass(TableModel tableModel, ClassDeclarationSyntax tClass)
    {
        var result = tClass.FindParentOrDefault<CompilationUnitSyntax>() 
                     ?? throw new SqExpressCodeGenException($"Could not find compilation unit for {tClass.Identifier.ValueText}");

        //Constructor
        var newClass = ReplaceAddProperties(tableModel,
            ReplaceConstructors(this, tableModel: tableModel, originalClass: tClass));

        return result.ReplaceNode(tClass, newClass).NormalizeWhitespace();

        static ClassDeclarationSyntax ReplaceConstructors(TableClassGenerator tableClassGenerator, TableModel tableModel, ClassDeclarationSyntax originalClass)
        {
            var newClass = originalClass;

            var mainConstructor = newClass.DescendantNodesAndSelf()
                .OfType<ConstructorDeclarationSyntax>()
                .FirstOrDefault(c =>
                    c.ParameterList.Parameters.Count == 1 &&
                    c.ParameterList.Parameters[0].Type?.ToString() == nameof(Alias) &&
                    c.Initializer != null &&
                    c.Initializer.Kind() == SyntaxKind.BaseConstructorInitializer &&
                    c.Initializer.ArgumentList.Arguments.Count == 3);

            var constructorDeclarationSyntax = tableClassGenerator.GenerateMainConstructor(tableModel);

            if (mainConstructor != null)
            {
                newClass = newClass.ReplaceNode(mainConstructor, constructorDeclarationSyntax);
            }

            var otherBaseConstructors = newClass.DescendantNodesAndSelf()
                .OfType<ConstructorDeclarationSyntax>()
                .Where(c =>
                    !c.IsEquivalentTo(constructorDeclarationSyntax) &&
                    c.Initializer != null &&
                    c.Initializer.Kind() == SyntaxKind.BaseConstructorInitializer)
                .ToList();

            if (otherBaseConstructors.Count > 0)
            {
                newClass = newClass.RemoveNodes(otherBaseConstructors, SyntaxRemoveOptions.KeepNoTrivia)!;
            }

            return newClass;
        }

        static ClassDeclarationSyntax ReplaceAddProperties(TableModel tableModel, ClassDeclarationSyntax newClass)
        {
            var columnsDict = tableModel.Columns.ToDictionary(c => c.Name);

            var replacements = new Dictionary<PropertyDeclarationSyntax, PropertyDeclarationSyntax>();

            foreach (var oldProperty in newClass.DescendantNodes()
                         .OfType<PropertyDeclarationSyntax>()
                         .Where(p => columnsDict.ContainsKey(p.Identifier.ValueText)))
            {
                var columnModel = columnsDict[oldProperty.Identifier.ValueText];

                columnsDict.Remove(oldProperty.Identifier.ValueText);

                var newProperty = GenerateColumnProperty(columnModel)
                    .WithAttributeLists(oldProperty.AttributeLists);

                replacements.Add(oldProperty, newProperty);
            }

            newClass = newClass.ReplaceNodes(replacements.Keys, (o, _) => replacements[o]);

            if (columnsDict.Count > 0)
            {
                newClass = newClass.AddMembers(tableModel
                    .Columns
                    .Where(c => columnsDict.ContainsKey(c.Name))
                    .Select(GenerateColumnProperty)
                    .Cast<MemberDeclarationSyntax>()
                    .ToArray());
            }

            return newClass;
        }
    }

    private ConstructorDeclarationSyntax GenerateEmptyConstructor(TableModel table)
    {
        return SyntaxFactory.ConstructorDeclaration(table.Name)
            .WithModifiers(Modifiers(SyntaxKind.PublicKeyword))
            .WithInitializer(SyntaxFactory.ConstructorInitializer(
                SyntaxKind.ThisConstructorInitializer, 
                argumentList: ArgumentList(("alias", SyntaxFactory.ParseName($"{nameof(SqExpress)}.{nameof(Alias)}").MemberAccess(nameof(Alias.Auto))))))
            .WithBody(SyntaxFactory.Block());
    }

    private ConstructorDeclarationSyntax GenerateMainConstructor(TableModel table)
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
            .WithBody(SyntaxFactory.Block(GenerateConstructorAssignments(table)));
    }

    private IEnumerable<Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax> GenerateConstructorAssignments(TableModel table)
    {
        foreach (var tableColumn in table.Columns)
        {
            yield return SyntaxFactory.ExpressionStatement(AssignmentThis(tableColumn.Name, tableColumn.ColumnType.Accept(ColumnFactoryGenerator.Instance, new ColumnContext(tableColumn, this._allTables))));
        }

        foreach (var tableIndex in table.Indexes)
        {
            Dictionary<ColumnRef, ColumnModel> allTableColumns = table.Columns.ToDictionary(i => i.DbName);

            yield return GenerateIndexFactory(tableIndex, allTableColumns);
        }
    }

    private PropertyDeclarationSyntax[] GenerateColumnProperties(TableModel table)
    {
        var result = new List<PropertyDeclarationSyntax>(table.Columns.Count);

        foreach (var tableColumn in table.Columns)
        {
            var propertyDeclaration = GenerateColumnProperty(tableColumn: tableColumn);

            result.Add(propertyDeclaration);
        }

        return result.ToArray();
    }

    private static PropertyDeclarationSyntax GenerateColumnProperty(ColumnModel tableColumn)
    {
        var propertyDeclaration = SyntaxFactory.PropertyDeclaration(
                tableColumn.ColumnType.Accept(ColumnTypeGenerator.Instance, null),
                tableColumn.Name)
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            .AddAccessorListAccessors(
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            );
        return propertyDeclaration;
    }

    private Microsoft.CodeAnalysis.CSharp.Syntax.StatementSyntax GenerateIndexFactory(IndexModel tableIndex, IReadOnlyDictionary<ColumnRef, ColumnModel> allTableColumns)
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
        var result = new List<MemberDeclarationSyntax>(2 + properties.Count)
        {
            empty, main
        };

        result.AddRange(properties);

        return result.ToArray();
    }

}