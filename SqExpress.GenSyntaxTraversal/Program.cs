using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SqExpress.GenSyntaxTraversal
{
    class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("Path to \"SqExpress\" project folder should be specified as the first argument");
                return 1;
            }

            string projDir = args[0];

            if (!Directory.Exists(projDir))
            {
                Console.WriteLine($"Directory \"{projDir}\" does not exist");
                return 2;
            }


            IReadOnlyList<NodeModel> buffer;
            try
            {
                buffer = BuildModelRoslyn(projDir);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not build model: {e.Message}");
                return 3;
            }

            try
            {
                Generate(projDir, @"SyntaxTreeOperations\ExprDeserializer.cs", buffer, GenerateDeserializer);
                Generate(projDir, @"SyntaxTreeOperations\Internal\ExprModifier.cs", buffer, GenerateModifier);
                Generate(projDir, @"SyntaxTreeOperations\Internal\ExprWalker.cs", buffer, GenerateWalker);
                Generate(projDir, @"SyntaxModifyExtensions.cs", buffer, GenerateSyntaxModify);
                Console.WriteLine("Done!");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return 4;
            }

            return 0;
        }


        private static void Generate(string projDir, string relativePath, IReadOnlyList<NodeModel> model, Action<IReadOnlyList<NodeModel>, StringBuilder> generator)
        {
            var path = Path.Combine(projDir, relativePath);

            StringBuilder newContentBuilder = new StringBuilder();

            bool skip = false;
            foreach (var line in File.ReadLines(path))
            {
                if (line.Contains("//CodeGenEnd"))
                {
                    generator.Invoke(model, newContentBuilder);
                    skip = false;
                }

                if (!skip)
                {
                    newContentBuilder.AppendLine(line);
                }

                if (line.Contains("//CodeGenStart"))
                {
                    skip = true;
                }
            }

            File.WriteAllText(path, newContentBuilder.ToString());
        }

        private static void GenerateDeserializer(IReadOnlyList<NodeModel> models, StringBuilder stringBuilder)
        {
            CodeBuilder builder = new CodeBuilder(stringBuilder, 4);
            foreach (var nodeModel in models)
            {
                builder.AppendStart(0, $"case \"{TypeTag(nodeModel.TypeName)}\": return ");
                if (nodeModel.IsSingleton)
                {
                    builder.AppendLine($"{nodeModel.TypeName}.Instance;");
                    continue;
                }
                builder.Append($"new {nodeModel.TypeName}(");
                for (var index = 0; index < nodeModel.SubNodes.Count; index++)
                {
                    var supNode = nodeModel.SubNodes[index];
                    if (index != 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append($"{supNode.ConstructorArgumentName}: Get{(supNode.IsNullable ? "Nullable" : null)}SubNode{(supNode.IsList ? "List" : null)}<TNode, {supNode.PropertyType}>(rootElement, reader, \"{supNode.PropertyName}\")");
                }

                for (var index = 0; index < nodeModel.Properties.Count; index++)
                {
                    var modelProperty = nodeModel.Properties[index];
                    if (index != 0 || nodeModel.SubNodes.Count > 0)
                    {
                        builder.Append(", ");
                    }
                    builder.Append($"{modelProperty.ConstructorArgumentName}: Read{GetPropertyTypeName(modelProperty: modelProperty)}(rootElement, reader, \"{modelProperty.PropertyName}\")");
                }

                builder.AppendLine(");");
            }

            static string GetPropertyTypeName(SubNodeModel modelProperty)
            {
                var result = modelProperty.PropertyType;

                if (modelProperty.IsNullable)
                {
                    result = "Nullable" + result;
                }

                if (modelProperty.IsList)
                {
                    result = result + "List";
                }

                return result;
            }
        }

        private static void GenerateModifier(IReadOnlyList<NodeModel> models, StringBuilder stringBuilder)
        {
            CodeBuilder builder = new CodeBuilder(stringBuilder, 2);
            foreach (var nodeModel in models)
            {
                builder.AppendLineStart(0, $"public IExpr? Visit{nodeModel.TypeName}({nodeModel.TypeName} exprIn, Func<IExpr, IExpr?> modifier)");
                builder.AppendLineStart(0, "{");

                if (nodeModel.SubNodes.Count > 0)
                {
                    foreach (var subNode in nodeModel.SubNodes)
                    {
                        if (!subNode.IsList)
                        {
                            builder.AppendLineStart(1, !subNode.IsNullable
                                ? $"var new{subNode.PropertyName} = this.AcceptItem(exprIn.{subNode.PropertyName}, modifier);"
                                : $"var new{subNode.PropertyName} = this.AcceptNullableItem(exprIn.{subNode.PropertyName}, modifier);");
                        }
                        else
                        {
                            builder.AppendLineStart(1, !subNode.IsNullable
                                ? $"var new{subNode.PropertyName} = this.AcceptNotNullCollection(exprIn.{subNode.PropertyName}, modifier);"
                                : $"var new{subNode.PropertyName} = this.AcceptNullCollection(exprIn.{subNode.PropertyName}, modifier);");
                        }
                    }

                    builder.AppendStart(1, "if(");
                    for (var index = 0; index < nodeModel.SubNodes.Count; index++)
                    {
                        var subNode = nodeModel.SubNodes[index];
                        if (index != 0)
                        {
                            builder.Append(" || ");
                        }

                        builder.Append($"!ReferenceEquals(exprIn.{subNode.PropertyName}, new{subNode.PropertyName})");
                    }

                    builder.AppendLine(")");
                    builder.AppendLineStart(1, "{");
                    builder.AppendStart(2, $"exprIn = new {nodeModel.TypeName}(");
                    for (var index = 0; index < nodeModel.SubNodes.Count; index++)
                    {
                        var subNode = nodeModel.SubNodes[index];
                        if (index != 0)
                        {
                            builder.Append(", ");
                        }

                        builder.Append($"{subNode.ConstructorArgumentName}: new{subNode.PropertyName}");
                    }
                    for (var index = 0; index < nodeModel.Properties.Count; index++)
                    {
                        var property = nodeModel.Properties[index];
                        if (index != 0 || nodeModel.SubNodes.Count > 0)
                        {
                            builder.Append(", ");
                        }

                        builder.Append($"{property.ConstructorArgumentName}: exprIn.{property.PropertyName}");
                    }

                    builder.AppendLine(");");
                    builder.AppendLineStart(1, "}");
                }
                builder.AppendLineStart(1, "return modifier.Invoke(exprIn);");
                builder.AppendLineStart(0, "}");
            }
        }

        private static void GenerateWalker(IReadOnlyList<NodeModel> models, StringBuilder stringBuilder)
        {
            CodeBuilder builder = new CodeBuilder(stringBuilder, 2);
            foreach (var nodeModel in models)
            {
                builder.AppendLineStart(0, $"public bool Visit{nodeModel.TypeName}({nodeModel.TypeName} expr, TCtx arg)");
                builder.AppendLineStart(0, "{");

                builder.AppendStart(1, $"var res = this.Visit(expr, \"{TypeTag(nodeModel.TypeName)}\", arg, out var argOut)");
                foreach (var subNode in nodeModel.SubNodes)
                {
                    builder.Append($" && this.Accept(\"{subNode.PropertyName}\",expr.{subNode.PropertyName}, argOut)");
                }

                builder.AppendLine(";");
                foreach (var subNode in nodeModel.Properties)
                {
                    builder.AppendLineStart(1, $"this.VisitPlainProperty(\"{subNode.PropertyName}\",expr.{subNode.PropertyName}, argOut);");
                }

                builder.AppendLineStart(1, "this._visitor.EndVisitExpr(expr, arg);");
                builder.AppendLineStart(1, "return res;");

                builder.AppendLineStart(0, "}");
            }
        }        
        
        private static void GenerateSyntaxModify(IReadOnlyList<NodeModel> models, StringBuilder stringBuilder)
        {
            CodeBuilder builder = new CodeBuilder(stringBuilder, 2);
            foreach (var nodeModel in models)
            {
                var subNodes = nodeModel.SubNodes.Concat(nodeModel.Properties).ToList();

                foreach (var subNode in subNodes)
                {
                    builder.AppendLineStart(0, $"public static {nodeModel.TypeName} With{subNode.PropertyName}(this {nodeModel.TypeName} original, {subNode.GetFullPropertyTypeName()} new{subNode.PropertyName}) ");
                    builder.AppendStart(1, $"=> new {nodeModel.TypeName}(");
                    for (var index = 0; index < subNodes.Count; index++)
                    {
                        var subNodeConst = subNodes[index];
                        if (index != 0)
                        {
                            builder.Append(", ");
                        }

                        builder.Append(subNodeConst == subNode
                            ? $"{subNode.ConstructorArgumentName}: new{subNode.PropertyName}"
                            : $"{subNodeConst.ConstructorArgumentName}: original.{subNodeConst.PropertyName}");
                    }

                    builder.AppendLine(");");
                    builder.AppendLine(null);
                }
            }
        }

        public static IReadOnlyList<NodeModel> BuildModelRoslyn(string projectFolder)
        {
            List<NodeModel> result = new List<NodeModel>();
				
            var files = Directory.EnumerateFiles(Path.Combine(projectFolder, "Syntax"), "*.cs", SearchOption.AllDirectories);

            files = files.Concat(Directory.EnumerateFiles(projectFolder, "IExpr*.cs"));

            var trees = files.Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f))).ToList();
            var cSharpCompilation = CSharpCompilation.Create("Syntax", trees);

            foreach (var tree in trees)
            {
                var semantic = cSharpCompilation.GetSemanticModel(tree);

                foreach (var classDeclarationSyntax in tree.GetRoot().DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>())
                {
                    var classSymbol = semantic.GetDeclaredSymbol(classDeclarationSyntax);

                    if (classSymbol != null && !classSymbol.IsAbstract && IsExpr(classSymbol) && classSymbol.Name.StartsWith("Expr"))
                    {
                        var properties = GetProperties(classSymbol);

                        var subNodes = new List<SubNodeModel>();
                        var modelProps = new List<SubNodeModel>();

                        foreach (var constructor in classSymbol.Constructors)
                        {
                            foreach (var parameter in constructor.Parameters)
                            {
                                INamedTypeSymbol pType = (INamedTypeSymbol)parameter.Type;

                                var correspondingProperty = properties.FirstOrDefault(prop =>
                                    string.Equals(prop.Name,
                                        parameter.Name,
                                        StringComparison.CurrentCultureIgnoreCase));

                                if (correspondingProperty == null)
                                {
                                    throw new Exception(
                                        $"Could not find a property for the constructor arg: '{parameter.Name}'");
                                }

                                var ta = AnalyzeSymbol(ref pType);

                                var subNodeModel = new SubNodeModel(correspondingProperty.Name, parameter.Name, pType.Name, ta.ListName, ta.IsNullable, ta.HostTypeName);
                                if (ta.Expr)
                                {
                                    subNodes.Add(subNodeModel);
                                }
                                else
                                {
                                    modelProps.Add(subNodeModel);
                                }

                            }
                        }

                        result.Add(new NodeModel(classSymbol.Name, modelProps.Count == 0 && subNodes.Count == 0, subNodes, modelProps));
                    }
                }
            }

            result.Sort((a, b) => string.CompareOrdinal(a.TypeName, b.TypeName));

            return result;

            bool IsExpr(INamedTypeSymbol symbol)
            {
                while (symbol != null)
                {
                    if (symbol.Interfaces.Any(HasA))
                    {
                        return true;
                    }
                    symbol = symbol.BaseType;
                }

                return false;


                bool HasA(INamedTypeSymbol iSym)
                {
                    if (iSym.Name == "IExpr")
                    {
                        return true;
                    }

                    return IsExpr(iSym);
                }
            }

            List<ISymbol> GetProperties(INamedTypeSymbol symbol)
            {
                List<ISymbol> result = new List<ISymbol>();
                while (symbol != null)
                {
                    result.AddRange(symbol.GetMembers().Where(m => m.Kind == SymbolKind.Property));
                    symbol = symbol.BaseType;
                }

                return result;
            }

            SymbolAnalysis AnalyzeSymbol(ref INamedTypeSymbol typeSymbol)
            {
                string listName = null;
                string hostType = null;
                if (typeSymbol.ContainingType != null)
                {
                    var host = typeSymbol.ContainingType;
                    hostType = host.Name;
                }

                var nullable = typeSymbol.NullableAnnotation == NullableAnnotation.Annotated;

                if (nullable && typeSymbol.Name == "Nullable")
                {
                    typeSymbol = (INamedTypeSymbol)typeSymbol.TypeArguments.Single();
                }

                if (typeSymbol.IsGenericType)
                {
                    if (typeSymbol.Name.Contains("List"))
                    {
                        listName = typeSymbol.Name;
                    }

                    if (typeSymbol.Name == "Nullable")
                    {
                        nullable = true;
                    }

                    typeSymbol = (INamedTypeSymbol)typeSymbol.TypeArguments.Single();
                }

                return new SymbolAnalysis(nullable, listName, IsExpr(typeSymbol), hostType);
            }
        }

        private static void Print(IReadOnlyList<NodeModel> buffer)
        {
            foreach (var nodeModel in buffer)
            {
                Console.WriteLine(nodeModel.TypeName);
                foreach (var sn in nodeModel.SubNodes)
                {
                    Console.WriteLine($"  - {sn.PropertyName}: {sn.PropertyType}{(sn.IsList ? "[]" : "")}");
                }

                foreach (var sn in nodeModel.Properties)
                {
                    Console.WriteLine($"  * {sn.PropertyName}: {sn.PropertyType}");
                }
            }
        }

        private static string TypeTag(string typeName)
        {
            if (!typeName.StartsWith("Expr"))
            {
                throw new Exception("Incorrect typename prefix");
            }

            return typeName.Substring(4);
        }


        class SymbolAnalysis
        {
            public readonly bool IsNullable;
            public readonly string ListName;
            public readonly bool Expr;
            public readonly string HostTypeName;

            public SymbolAnalysis(bool isNullable, string listName, bool expr, string hostTypeName)
            {
                this.IsNullable = isNullable;
                this.ListName = listName;
                this.Expr = expr;
                this.HostTypeName = hostTypeName;
            }
        }
    }

    public class NodeModel
    {
        public NodeModel(string typeName, bool isSingleton, IReadOnlyList<SubNodeModel> subNodes, IReadOnlyList<SubNodeModel> properties)
        {
            this.TypeName = typeName;
            this.IsSingleton = isSingleton;
            this.SubNodes = subNodes;
            this.Properties = properties;
        }

        public string TypeName { get; }

        public bool IsSingleton { get; }

        public IReadOnlyList<SubNodeModel> SubNodes { get; }

        public IReadOnlyList<SubNodeModel> Properties { get; }
    }

    public class SubNodeModel
    {
        public SubNodeModel(string propertyName, string constructorArgumentName, string propertyType, string listName, bool isNullable, string hostTypeName)
        {
            this.PropertyName = propertyName;
            this.PropertyType = propertyType;
            this.ListName = listName;
            this.IsNullable = isNullable;
            this.HostTypeName = hostTypeName;
            this.ConstructorArgumentName = constructorArgumentName;
        }

        public string PropertyName { get; }

        public string ConstructorArgumentName { get; }

        public string PropertyType { get; }

        public string ListName { get; }

        public bool IsList => this.ListName != null;

        public bool IsNullable { get; }

        public string HostTypeName { get; }


        public string GetFullPropertyTypeName()
        {
            string res = this.PropertyType;

            if (!string.IsNullOrEmpty(this.HostTypeName))
            {
                res = $"{this.HostTypeName}.{res}";
            }

            if (this.IsList)
            {
                res = $"{this.ListName}<{res}>";
            }
            if (this.IsNullable)
            {
                res += "?";
            }

            return res;
        }

    }

    public class CodeBuilder
    {
        private readonly StringBuilder _builder;

        private readonly int _indentTabs;

        public CodeBuilder(StringBuilder builder, int indentTabs)
        {
            this._builder = builder;
            this._indentTabs = indentTabs;
        }

        public void AppendLine(string line)
        {
            this._builder.AppendLine(line);
        }

        public void AppendLineStart(int tabs, string line)
        {
            this._builder.Append(' ', (this._indentTabs + tabs) * 4);
            this._builder.AppendLine(line);
        }

        public void AppendStart(int tabs, string line)
        {
            this._builder.Append(' ', (this._indentTabs + tabs) * 4);
            this._builder.Append(line);
        }

        public void Append(string line)
        {
            this._builder.Append(line);
        }
    }
}
