using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.DbMetadata.Internal.Model;
using SqExpress;

namespace SqExpress.CodeGen.Shared
{
    internal static class CodeGenLegacySqModelSupport
    {
        private static readonly string SqModelAttributeShortName = nameof(SqModelAttribute)
            .Substring(0, nameof(SqModelAttribute).Length - nameof(Attribute).Length);

        public static IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> FindTableDescriptors(string path, IFileSystem fileSystem)
        {
            var result = new Dictionary<TableRef, ClassDeclarationSyntax>();
            foreach (var item in EnumerateSyntaxTrees(path, fileSystem).ExploreTableDescriptors())
            {
                if (item.TableRef != null && !result.ContainsKey(item.TableRef))
                {
                    result.Add(item.TableRef, item.ClassDeclaration);
                }
            }

            return result;
        }

        public static IReadOnlyList<CodeGenSqModelMeta> AnalyzeLegacySqModels(string path, IFileSystem fileSystem, bool nullRefTypes)
        {
            var models = new Dictionary<string, CodeGenSqModelMeta>(StringComparer.Ordinal);

            foreach (var attribute in EnumerateLegacySqModelAttributes(path, fileSystem))
            {
                var propertySyntax = attribute.FindParentOrDefault<PropertyDeclarationSyntax>()
                    ?? throw new InvalidOperationException("Could not find property declaration for SqModel attribute.");
                var classSyntax = propertySyntax.FindParentOrDefault<ClassDeclarationSyntax>()
                    ?? throw new InvalidOperationException("Could not find class declaration for SqModel attribute.");
                var namespaceSyntax = classSyntax.FindParentOrDefault<NamespaceDeclarationSyntax>();
                var baseTypeKindTag = GetTableClassKind(classSyntax)
                    ?? throw new InvalidOperationException($"Unknown base class in '{classSyntax.Identifier.ValueText}'.");

                var modelName = (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression as LiteralExpressionSyntax)?.Token.ValueText
                                ?? throw new InvalidOperationException($"Could not find {nameof(SqModelAttribute)} name parameter.");

                var modelPropertyName =
                    GetAttributeProperty<LiteralExpressionSyntax>(attribute, nameof(SqModelAttribute.PropertyName))
                        ?.Token.ValueText
                    ?? propertySyntax.Identifier.ValueText;

                var castType =
                    GetAttributeProperty<TypeOfExpressionSyntax>(attribute, nameof(SqModelAttribute.CastType))
                        ?.Type.ToString();

                if (!TryParseTableColumnKind(propertySyntax.Type.ToString(), out var columnKind))
                {
                    throw new InvalidOperationException($"Unknown column type: \"{propertySyntax.Type}\".");
                }

                var clrType = CodeGenModelSupport.GetClrTypeName(columnKind, nullRefTypes);
                var (isPrimaryKey, isIdentity) = AnalyzeColumnMetadata(classSyntax, propertySyntax.Identifier.ValueText);

                if (!models.TryGetValue(modelName, out var meta))
                {
                    meta = new CodeGenSqModelMeta(modelName);
                    models.Add(modelName, meta);
                }

                var property = meta.AddPropertyCheckExistence(new CodeGenSqModelPropertyMeta(
                    modelPropertyName,
                    clrType,
                    castType,
                    isPrimaryKey,
                    isIdentity));

                property.AddColumnCheckExistence(
                    meta.Name,
                    new CodeGenSqModelPropertyTableColMeta(
                        new CodeGenSqModelTableRef(
                            classSyntax.Identifier.ValueText,
                            namespaceSyntax?.Name.ToString() ?? string.Empty,
                            baseTypeKindTag),
                        propertySyntax.Identifier.ValueText));
            }

            var ordered = models.Values.OrderBy(static i => i.Name, StringComparer.Ordinal).ToList();
            foreach (var model in ordered)
            {
                int? count = null;
                foreach (var property in model.Properties)
                {
                    if (count == null)
                    {
                        count = property.Column.Count;
                    }
                    else if (count.Value != property.Column.Count)
                    {
                        throw new InvalidOperationException($"{nameof(SqModelAttribute)} with name \"{model.Name}\" was declared in several table descriptors but numbers of properties do not match.");
                    }
                }
            }

            return ordered;
        }

        private static IEnumerable<AttributeSyntax> EnumerateLegacySqModelAttributes(string path, IFileSystem fileSystem)
        {
            foreach (var item in EnumerateSyntaxTrees(path, fileSystem).ExploreTableDescriptors())
            {
                foreach (var attribute in item.ClassDeclaration.DescendantNodes()
                             .OfType<PropertyDeclarationSyntax>()
                             .SelectMany(static p => p.DescendantNodes())
                             .OfType<AttributeSyntax>())
                {
                    var name = attribute.Name.ToString();
                    if (name.EndsWith(SqModelAttributeShortName, StringComparison.Ordinal)
                        || name.EndsWith(nameof(SqModelAttribute), StringComparison.Ordinal))
                    {
                        yield return attribute;
                    }
                }
            }
        }

        private static IEnumerable<SyntaxTree> EnumerateSyntaxTrees(string path, IFileSystem fileSystem)
        {
            if (!fileSystem.DirectoryExists(path))
            {
                throw new InvalidOperationException($"Directory \"{path}\" does not exits.");
            }

            return fileSystem
                .EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
                .Select(f => CSharpSyntaxTree.ParseText(fileSystem.ReadAllText(f)));
        }

        private static IEnumerable<CodeDescriptorPath> ExploreTableDescriptors(this IEnumerable<SyntaxTree> syntaxTrees)
        {
            foreach (var syntaxTree in syntaxTrees)
            {
                var classes = syntaxTree.GetRoot()
                    .DescendantNodesAndSelf()
                    .OfType<ClassDeclarationSyntax>()
                    .Select(cd => (Class: cd, BaseTypeKind: GetTableClassKind(cd)))
                    .Where(static p => p.BaseTypeKind != null);

                foreach (var tuple in classes)
                {
                    var baseConstCall = tuple.Class
                        .DescendantNodes()
                        .OfType<ConstructorInitializerSyntax>()
                        .FirstOrDefault(static c => c.Kind() == SyntaxKind.BaseConstructorInitializer);

                    var baseTypeKindTag = tuple.BaseTypeKind!.Value;
                    if (baseTypeKindTag == BaseTypeKindTag.DerivedTableBase)
                    {
                        yield return new CodeDescriptorPath(tuple.Class, baseTypeKindTag, null);
                        continue;
                    }

                    if (baseConstCall == null)
                    {
                        throw new InvalidOperationException($"Unexpected base type kind: '{baseTypeKindTag}' (with empty base constructor).");
                    }

                    string schema;
                    string tableName;

                    if (baseTypeKindTag == BaseTypeKindTag.TableBase)
                    {
                        if (baseConstCall.ArgumentList.Arguments.Count != 3
                            || !(baseConstCall.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax schemaLiteral)
                            || schemaLiteral.Kind() != SyntaxKind.StringLiteralExpression
                            || !(baseConstCall.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax tableLiteral)
                            || tableLiteral.Kind() != SyntaxKind.StringLiteralExpression)
                        {
                            continue;
                        }

                        schema = schemaLiteral.Token.ValueText;
                        tableName = tableLiteral.Token.ValueText;
                    }
                    else if (baseTypeKindTag == BaseTypeKindTag.TempTableBase)
                    {
                        if (baseConstCall.ArgumentList.Arguments.Count != 2
                            || !(baseConstCall.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax tableLiteral)
                            || tableLiteral.Kind() != SyntaxKind.StringLiteralExpression)
                        {
                            continue;
                        }

                        schema = string.Empty;
                        tableName = tableLiteral.Token.ValueText;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unknown base type kind: '{baseTypeKindTag}'.");
                    }

                    yield return new CodeDescriptorPath(tuple.Class, baseTypeKindTag, new TableRef(schema, tableName));
                }
            }
        }

        private static (bool Pk, bool Identity) AnalyzeColumnMetadata(ClassDeclarationSyntax classSyntax, string columnName)
        {
            var assignment = classSyntax
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .SelectMany(static cd => cd.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                .FirstOrDefault(a =>
                    a.Left.DescendantNodesAndSelf()
                        .OfType<IdentifierNameSyntax>()
                        .Any(i => i.Identifier.ValueText == columnName));

            if (assignment == null)
            {
                return default;
            }

            var memberAccesses = assignment.Right.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();
            return (
                memberAccesses.Any(static ma => ma.Name.Identifier.ValueText == nameof(ColumnMeta.ColumnMetaBuilder.PrimaryKey)),
                memberAccesses.Any(static ma => ma.Name.Identifier.ValueText == nameof(ColumnMeta.ColumnMetaBuilder.Identity)));
        }

        private static BaseTypeKindTag? GetTableClassKind(ClassDeclarationSyntax classDeclaration)
        {
            return classDeclaration.BaseList?.DescendantNodesAndSelf()
                .OfType<BaseTypeSyntax>()
                .Select(static baseType =>
                {
                    switch (baseType.Type.ToString())
                    {
                        case nameof(TableBase): return (BaseTypeKindTag?)BaseTypeKindTag.TableBase;
                        case nameof(TempTableBase): return BaseTypeKindTag.TempTableBase;
                        case nameof(DerivedTableBase): return BaseTypeKindTag.DerivedTableBase;
                        default: return null;
                    }
                })
                .FirstOrDefault();
        }

        private static T? GetAttributeProperty<T>(AttributeSyntax attribute, string name)
            where T : ExpressionSyntax
        {
            return attribute
                .ArgumentList?
                .Arguments
                .Where(a => a.NameEquals?.Name.ToString() == name)
                .Select(static a => a.Expression)
                .OfType<T>()
                .FirstOrDefault();
        }

        private static bool TryParseTableColumnKind(string typeName, out CodeGenColumnKind kind)
        {
            switch (typeName)
            {
                case nameof(BooleanTableColumn):
                case nameof(BooleanCustomColumn):
                    kind = CodeGenColumnKind.Boolean;
                    return true;
                case nameof(NullableBooleanTableColumn):
                case nameof(NullableBooleanCustomColumn):
                    kind = CodeGenColumnKind.NullableBoolean;
                    return true;
                case nameof(ByteTableColumn):
                case nameof(ByteCustomColumn):
                    kind = CodeGenColumnKind.Byte;
                    return true;
                case nameof(NullableByteTableColumn):
                case nameof(NullableByteCustomColumn):
                    kind = CodeGenColumnKind.NullableByte;
                    return true;
                case nameof(ByteArrayTableColumn):
                case nameof(ByteArrayCustomColumn):
                    kind = CodeGenColumnKind.ByteArray;
                    return true;
                case nameof(NullableByteArrayTableColumn):
                case nameof(NullableByteArrayCustomColumn):
                    kind = CodeGenColumnKind.NullableByteArray;
                    return true;
                case nameof(Int16TableColumn):
                case nameof(Int16CustomColumn):
                    kind = CodeGenColumnKind.Int16;
                    return true;
                case nameof(NullableInt16TableColumn):
                case nameof(NullableInt16CustomColumn):
                    kind = CodeGenColumnKind.NullableInt16;
                    return true;
                case nameof(Int32TableColumn):
                case nameof(Int32CustomColumn):
                    kind = CodeGenColumnKind.Int32;
                    return true;
                case nameof(NullableInt32TableColumn):
                case nameof(NullableInt32CustomColumn):
                    kind = CodeGenColumnKind.NullableInt32;
                    return true;
                case nameof(Int64TableColumn):
                case nameof(Int64CustomColumn):
                    kind = CodeGenColumnKind.Int64;
                    return true;
                case nameof(NullableInt64TableColumn):
                case nameof(NullableInt64CustomColumn):
                    kind = CodeGenColumnKind.NullableInt64;
                    return true;
                case nameof(DecimalTableColumn):
                case nameof(DecimalCustomColumn):
                    kind = CodeGenColumnKind.Decimal;
                    return true;
                case nameof(NullableDecimalTableColumn):
                case nameof(NullableDecimalCustomColumn):
                    kind = CodeGenColumnKind.NullableDecimal;
                    return true;
                case nameof(DoubleTableColumn):
                case nameof(DoubleCustomColumn):
                    kind = CodeGenColumnKind.Double;
                    return true;
                case nameof(NullableDoubleTableColumn):
                case nameof(NullableDoubleCustomColumn):
                    kind = CodeGenColumnKind.NullableDouble;
                    return true;
                case nameof(DateTimeTableColumn):
                case nameof(DateTimeCustomColumn):
                    kind = CodeGenColumnKind.DateTime;
                    return true;
                case nameof(NullableDateTimeTableColumn):
                case nameof(NullableDateTimeCustomColumn):
                    kind = CodeGenColumnKind.NullableDateTime;
                    return true;
                case nameof(DateTimeOffsetTableColumn):
                case nameof(DateTimeOffsetCustomColumn):
                    kind = CodeGenColumnKind.DateTimeOffset;
                    return true;
                case nameof(NullableDateTimeOffsetTableColumn):
                case nameof(NullableDateTimeOffsetCustomColumn):
                    kind = CodeGenColumnKind.NullableDateTimeOffset;
                    return true;
                case nameof(GuidTableColumn):
                case nameof(GuidCustomColumn):
                    kind = CodeGenColumnKind.Guid;
                    return true;
                case nameof(NullableGuidTableColumn):
                case nameof(NullableGuidCustomColumn):
                    kind = CodeGenColumnKind.NullableGuid;
                    return true;
                case nameof(StringTableColumn):
                case nameof(StringCustomColumn):
                    kind = CodeGenColumnKind.String;
                    return true;
                case nameof(NullableStringTableColumn):
                case nameof(NullableStringCustomColumn):
                    kind = CodeGenColumnKind.NullableString;
                    return true;
                default:
                    kind = default;
                    return false;
            }
        }

        private readonly struct CodeDescriptorPath
        {
            public CodeDescriptorPath(ClassDeclarationSyntax classDeclaration, BaseTypeKindTag kindTag, TableRef? tableRef)
            {
                this.ClassDeclaration = classDeclaration;
                this.KindTag = kindTag;
                this.TableRef = tableRef;
            }

            public ClassDeclarationSyntax ClassDeclaration { get; }

            public BaseTypeKindTag KindTag { get; }

            public TableRef? TableRef { get; }
        }
    }
}
