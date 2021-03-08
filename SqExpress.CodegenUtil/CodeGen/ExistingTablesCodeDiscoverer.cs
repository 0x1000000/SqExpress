using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SqExpress.CodeGenUtil.Model;

namespace SqExpress.CodeGenUtil.CodeGen
{
    internal class ExistingTablesCodeDiscoverer
    {
        public static IReadOnlyDictionary<TableRef, ClassDeclarationSyntax> Discover(string path)
        {
            if (!Directory.Exists(path))
            {
                throw new SqExpressCodeGenException($"Directory \"{path}\" does not exits");
            }

            var files = Directory.EnumerateFiles(
                path,
                "*.cs",
                SearchOption.AllDirectories);


            var trees = files
                .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)))
                .ToList();

            var classes = trees
                .SelectMany(t => t.GetRoot().DescendantNodesAndSelf())
                .OfType<ClassDeclarationSyntax>()
                .Where(cd =>
                    cd.BaseList?.DescendantNodesAndSelf()
                        .OfType<BaseTypeSyntax>()
                        .Any(b => b.Type.ToString().EndsWith(nameof(TableBase))) ??
                    false);

            var result = new Dictionary<TableRef, ClassDeclarationSyntax>();
            foreach (var classDeclarationSyntax in classes)
            {
                var baseConstCall = classDeclarationSyntax
                    .DescendantNodes()
                    .OfType<ConstructorInitializerSyntax>()
                    .FirstOrDefault(c =>
                        c.Kind() == SyntaxKind.BaseConstructorInitializer && c.ArgumentList.Arguments.Count == 3);
                if (baseConstCall != null)
                {
                    string schema;
                    string tableName;
                    if (baseConstCall.ArgumentList.Arguments[0].Expression is LiteralExpressionSyntax slSh &&
                        slSh.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        schema = slSh.Token.ValueText;
                    }
                    else
                    {
                        continue;
                    }

                    if (baseConstCall.ArgumentList.Arguments[1].Expression is LiteralExpressionSyntax slDb &&
                        slDb.Kind() == SyntaxKind.StringLiteralExpression)
                    {
                        tableName = slDb.Token.ValueText;
                    }
                    else
                    {
                        continue;
                    }

                    TableRef tableRef = new TableRef(schema, tableName);
                    if (!result.ContainsKey(tableRef))
                    {
                        result.Add(tableRef, classDeclarationSyntax);
                    }
                }
            }

            return result;
        }
    }

}