using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGenUtil.Model.SqModel;

namespace SqExpress.CodeGenUtil.CodeGen
{
    internal static class SqModelAttributeAnalysis
    {
        public static IEnumerable<SqModelMetaRaw> ParseAttribute(this IEnumerable<AttributeSyntax> attributes, bool nullRefTypes)
        {
            foreach (var attribute in attributes)
            {
                var propertySyntax = attribute.FindParentOrDefault<PropertyDeclarationSyntax>()!;

                var classSyntax = propertySyntax.FindParentOrDefault<ClassDeclarationSyntax>()!;



                var namespaceSyntax = classSyntax.FindParentOrDefault<NamespaceDeclarationSyntax>()!;

                var tableNamespace = namespaceSyntax.Name.ToString();

                var tableName = classSyntax.Identifier.ValueText;

                var columnName = propertySyntax.Identifier.ValueText;

                var identity = AnalyzeColumnMetadata(classSyntax: classSyntax, columnName: columnName);

                var modelName = (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression as LiteralExpressionSyntax)?.Token.ValueText
                                ?? throw new SqExpressCodeGenException($"Could not find {nameof(SqModelAttribute)} name parameter");

                var modelPropertyName =
                    GetAttributeProperty<LiteralExpressionSyntax>(attribute, nameof(SqModelAttribute.PropertyName))
                        ?.Token.ValueText ??
                    columnName;

                var castType =
                    GetAttributeProperty<TypeOfExpressionSyntax>(attribute, nameof(SqModelAttribute.CastType))
                        ?.Type.ToString();

                var clrType = ColumnPropertyTypeParser.Parse(
                    propertySyntax.Type.ToString(),
                    ModelColumnClrTypeGenerator.Instance,
                    nullRefTypes);

                yield return new SqModelMetaRaw(
                    modelName: modelName,
                    fieldName: modelPropertyName,
                    fieldTypeName: clrType,
                    castTypeName: castType,
                    tableNamespace: tableNamespace,
                    tableName: tableName,
                    columnName: columnName,
                    isPrimaryKey: identity.Pk,
                    isIdentity: identity.Idendity);
            }

            static T? GetAttributeProperty<T>(AttributeSyntax attribute, string name) where T : ExpressionSyntax
            {
                return attribute
                    .ArgumentList?
                    .Arguments
                    .Where(a => a.NameEquals?.Name.ToString() == name)
                    .Select(a => a.Expression)
                    .OfType<T>()
                    .FirstOrDefault();
            }
        }

        private static (bool Pk, bool Idendity) AnalyzeColumnMetadata(ClassDeclarationSyntax classSyntax, string columnName)
        {
            var assignment = classSyntax
                .DescendantNodes()
                .OfType<ConstructorDeclarationSyntax>()
                .SelectMany(cd => cd.DescendantNodes().OfType<AssignmentExpressionSyntax>())
                .FirstOrDefault(a =>
                    a.Left.DescendantNodesAndSelf()
                        .OfType<IdentifierNameSyntax>()
                        .Any(i => i.Identifier.ValueText == columnName));

            bool identity = false;
            bool key = false;

            if (assignment != null)
            {
                var mas = assignment.Right.DescendantNodes().OfType<MemberAccessExpressionSyntax>().ToList();

                identity = mas.Any(ma => ma.Name.Identifier.ValueText == nameof(ColumnMeta.ColumnMetaBuilder.Identity));
                key = mas.Any(ma => ma.Name.Identifier.ValueText == nameof(ColumnMeta.ColumnMetaBuilder.PrimaryKey));
            }

            return (key, identity);
        }

        public static IReadOnlyList<SqModelMeta> CreateAnalysis(this IEnumerable<SqModelMetaRaw> rawModels)
        {
            var acc = new Dictionary<string, SqModelMeta>();

            foreach (var raw in rawModels)
            {
                var meta = GetFromAcc(raw);

                var property = meta.AddPropertyCheckExistence(new SqModelPropertyMeta(raw.FieldName, raw.FieldTypeName, raw.CastTypeName, raw.IsPrimaryKey, raw.IsIdentity));

                property.AddColumnCheckExistence(meta.Name, new SqModelPropertyTableColMeta(new SqModelTableRef(raw.TableName, raw.TableNamespace), raw.ColumnName));
            }

            var res =  acc.Values.OrderBy(v => v.Name).ToList();

            foreach (var model in res)
            {
                int? num = null;

                foreach (var p in model.Properties)
                {
                    if (num == null)
                    {
                        num = p.Column.Count;
                    }
                    else
                    {
                        if (num.Value != p.Column.Count)
                        {
                            throw new SqExpressCodeGenException($"{nameof(SqModelAttribute)} with name \"{model.Name}\" was declared in several table descriptors but numbers of properties do not match");
                        }
                    }
                }
            }


            return res;

            SqModelMeta GetFromAcc(SqModelMetaRaw raw)
            {
                if (acc.TryGetValue(raw.ModelName, out var result))
                {
                    return result;
                }

                var meta = new SqModelMeta(raw.ModelName);
                acc.Add(raw.ModelName, meta);
                return meta;
            }
            

        }

    }
}