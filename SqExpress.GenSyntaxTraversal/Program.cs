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
                Generate(projDir, @"Syntax\IExprVisitorNoArg.cs", buffer, GenerateVisitorInterfaceNoArg);
                Generate(projDir, @"Syntax\ExprVisitorProxy.cs", buffer, GenerateVisitorProxy);
                Generate(projDir, @"SyntaxTreeOperations\ExprDeserializer.cs", buffer, GenerateDeserializer);
                Generate(projDir, @"SyntaxTreeOperations\ExprVisitorBase.cs", buffer, GenerateVisitorBaseNonGeneric);
                Generate(projDir, @"SyntaxTreeOperations\Internal\ExprModifier.cs", buffer, GenerateModifier);
                Generate(projDir, @"SyntaxTreeOperations\Internal\ExprWalker.cs", buffer, GenerateWalker);
                Generate(projDir, @"SyntaxTreeOperations\Internal\ExprWalkerPull.cs", buffer, GenerateWalkerPull);
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
                string pr = null;
                if (nodeModel.IsCustomTraversal)
                {
                    pr = "//";
                    builder.AppendLineStart(0, "////Default implementation");
                }

                builder.AppendStart(0, $"{pr}case \"{TypeTag(nodeModel.TypeName)}\": return ");
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

                if (nodeModel.IsCustomTraversal)
                {
                    builder.AppendStart(0, $"case \"{TypeTag(nodeModel.TypeName)}\": return Build{TypeTag(nodeModel.TypeName)}(rootElement, reader)");
                    builder.AppendLine(";");
                }
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
                string pr = null;
                if (nodeModel.IsCustomTraversal)
                {
                    pr = "//";
                    builder.AppendLineStart(0, "////Default implementation");
                }
                builder.AppendLineStart(0, $"{pr}public IExpr? Visit{nodeModel.TypeName}({nodeModel.TypeName} exprIn, Func<IExpr, IExpr?> modifier)");
                builder.AppendLineStart(0, $"{pr}{{");

                if (nodeModel.SubNodes.Count > 0)
                {
                    foreach (var subNode in nodeModel.SubNodes)
                    {
                        if (!subNode.IsList)
                        {
                            builder.AppendLineStart(1, !subNode.IsNullable
                                ? $"{pr}var new{subNode.PropertyName} = this.AcceptItem(exprIn.{subNode.PropertyName}, modifier);"
                                : $"{pr}var new{subNode.PropertyName} = this.AcceptNullableItem(exprIn.{subNode.PropertyName}, modifier);");
                        }
                        else
                        {
                            builder.AppendLineStart(1, !subNode.IsNullable
                                ? $"{pr}var new{subNode.PropertyName} = this.AcceptNotNullCollection(exprIn.{subNode.PropertyName}, modifier);"
                                : $"{pr}var new{subNode.PropertyName} = this.AcceptNullCollection(exprIn.{subNode.PropertyName}, modifier);");
                        }
                    }

                    builder.AppendStart(1, $"{pr}if(");
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
                    builder.AppendLineStart(1, $"{pr}{{");
                    builder.AppendStart(2, $"{pr}exprIn = new {nodeModel.TypeName}(");
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
                    builder.AppendLineStart(1, $"{pr}}}");
                }
                builder.AppendLineStart(1, $"{pr}return modifier.Invoke(exprIn);");
                builder.AppendLineStart(0, $"{pr}}}");
            }
        }

        private static void GenerateWalker(IReadOnlyList<NodeModel> models, StringBuilder stringBuilder)
        {
            CodeBuilder builder = new CodeBuilder(stringBuilder, 2);
            foreach (var nodeModel in models)
            {
                string pr = null;
                if (nodeModel.IsCustomTraversal)
                {
                    pr = "//";
                    builder.AppendLineStart(0, "////Default implementation");
                }

                var hasSubNodes = nodeModel.SubNodes.Count > 0;

                builder.AppendLineStart(0, $"{pr}public bool Visit{nodeModel.TypeName}({nodeModel.TypeName} expr, WalkerContext<TCtx> arg)");
                builder.AppendLineStart(0, $"{pr}{{");
                if (hasSubNodes)
                {
                    builder.AppendLineStart(1, $"{pr}var res = true;");
                }
                builder.AppendLineStart(1, $"{pr}var walkResult = this.Visit(expr, \"{TypeTag(nodeModel.TypeName)}\", arg, out var argOut);");
                if (hasSubNodes)
                {
                    builder.AppendLineStart(1, $"{pr}if(walkResult == WalkResult.Continue)");
                    builder.AppendLineStart(1, $"{pr}{{");
                    builder.AppendStart(2, $"{pr}res = ");
                    for (var index = 0; index < nodeModel.SubNodes.Count; index++)
                    {
                        var subNode = nodeModel.SubNodes[index];
                        if (index != 0)
                        {
                            builder.Append(" && ");
                        }
                        builder.Append($"this.Accept(\"{subNode.PropertyName}\",expr.{subNode.PropertyName}, argOut)");
                    }

                    builder.AppendLine(";");

                    builder.AppendLineStart(1, $"{pr}}}");
                }

                foreach (var subNode in nodeModel.Properties)
                {
                    builder.AppendLineStart(1, $"{pr}this.VisitPlainProperty(\"{subNode.PropertyName}\",expr.{subNode.PropertyName}, argOut.Context);");
                }

                builder.AppendLineStart(1, $"{pr}this.EndVisit(expr, argOut.Context);");
                if (hasSubNodes)
                {
                    builder.AppendLineStart(1, $"{pr}return res && walkResult != WalkResult.Stop;");
                }
                else
                {
                    builder.AppendLineStart(1, $"{pr}return walkResult != WalkResult.Stop;");
                }

                builder.AppendLineStart(0, $"{pr}}}");
            }
        }        

        private static void GenerateWalkerPull(IReadOnlyList<NodeModel> models, StringBuilder stringBuilder)
        {
            CodeBuilder builder = new CodeBuilder(stringBuilder, 2);
            foreach (var nodeModel in models)
            {
                string pr = null;
                if (nodeModel.IsCustomTraversal)
                {
                    pr = "//";
                    builder.AppendLineStart(0, "////Default implementation");
                }

                builder.AppendLineStart(0, $"{pr}public bool Visit{nodeModel.TypeName}({nodeModel.TypeName} expr, object? arg)");
                builder.AppendLineStart(0, $"{pr}{{");


                builder.AppendLineStart(1, $"{pr}switch (this.Peek().State)");
                builder.AppendLineStart(1, $"{pr}{{");

                var index = 0;
                for (; index < nodeModel.SubNodes.Count; index++)
                {
                    var subNode = nodeModel.SubNodes[index];
                    builder.AppendLineStart(2, $"{pr}case {index+1}:");
                    builder.AppendLineStart(3, $"{pr}return this.SetCurrent(expr.{subNode.PropertyName});");
                }
                builder.AppendLineStart(2, $"{pr}case {index + 1}:");
                builder.AppendLineStart(3, $"{pr}return this.Pop();");

                builder.AppendLineStart(2, $"{pr}default:");
                builder.AppendLineStart(3, $"{pr}throw new SqExpressException(\"Incorrect enumerator visitor state\");");

                builder.AppendLineStart(1, $"{pr}}}");
                builder.AppendLineStart(0, $"{pr}}}");
            }
        }

        private static void GenerateVisitorBaseNonGeneric(IReadOnlyList<NodeModel> models, StringBuilder stringBuilder)
        {
            CodeBuilder builder = new CodeBuilder(stringBuilder, 2);
            foreach (var nodeModel in models)
            {
                string pr = null;
                if (nodeModel.IsCustomTraversal)
                {
                    pr = "//";
                    builder.AppendLineStart(0, "////Default implementation");
                }

                builder.AppendLineStart(0, $"{pr}public virtual void Visit{nodeModel.TypeName}({nodeModel.TypeName} expr)");
                builder.AppendLineStart(0, $"{pr}{{");

                if (nodeModel.SubNodes.Count == 0)
                {
                    builder.AppendLineStart(1, $"{pr}");
                }
                else
                {
                    for (var index = 0; index < nodeModel.SubNodes.Count; index++)
                    {
                        var subNode = nodeModel.SubNodes[index];
                        builder.AppendLineStart(1, $"{pr}this.Accept(expr.{subNode.PropertyName});");
                    }                    
                }

                builder.AppendLineStart(0, $"{pr}}}");
            }
        }

        private static void GenerateVisitorInterfaceNoArg(IReadOnlyList<NodeModel> models, StringBuilder stringBuilder)
        {
            CodeBuilder builder = new CodeBuilder(stringBuilder, 2);
            foreach (var nodeModel in models)
            {
                builder.AppendLineStart(0, $"void Visit{nodeModel.TypeName}({nodeModel.TypeName} expr);");
            }
        }

        private static void GenerateVisitorProxy(IReadOnlyList<NodeModel> models, StringBuilder stringBuilder)
        {
            CodeBuilder builder = new CodeBuilder(stringBuilder, 2);
            foreach (var nodeModel in models)
            {
                builder.AppendLineStart(0, $"public object? Visit{nodeModel.TypeName}({nodeModel.TypeName} expr, object? arg)");
                builder.AppendLineStart(0, "{");
                builder.AppendLineStart(1, "this._nodeHandler?.OnEnterNode(expr);");
                builder.AppendLineStart(1, "try");
                builder.AppendLineStart(1, "{");
                builder.AppendLineStart(2, $"this._visitor.Visit{nodeModel.TypeName}(expr);");
                builder.AppendLineStart(2, "return null;");
                builder.AppendLineStart(1, "}");
                builder.AppendLineStart(1, "finally");
                builder.AppendLineStart(1, "{");
                builder.AppendLineStart(2, "this._nodeHandler?.OnLeaveNode();");
                builder.AppendLineStart(1, "}");
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

                    var isSuitable = classSymbol != null 
                                 && !classSymbol.IsAbstract 
                                 && classSymbol.DeclaredAccessibility == Accessibility.Public
                                 && IsExpr(classSymbol) 
                                 && classSymbol.Name.StartsWith("Expr");
                        
                    if (!isSuitable)
                    {
                        continue;
                    }

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
                                    $"Could not find a property for the constructor arg: '{parameter.Name}' in {classSymbol.Name}");
                            }

                            var ta = AnalyzeSymbol(ref pType);

                            var subNodeModel = new SubNodeModel(correspondingProperty.Name,
                                parameter.Name,
                                pType.Name,
                                ta.ListName,
                                ta.IsNullable,
                                ta.HostTypeName);
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

                    var isCustomTraversal = classSymbol.GetAttributes().Any(a => a.AttributeClass?.Name == "SqCustomTraversalAttribute");

                    result.Add(new NodeModel(classSymbol.Name,
                        modelProps.Count == 0 && subNodes.Count == 0,
                        isCustomTraversal,
                        subNodes,
                        modelProps));
                }
            }

            result.Sort((a, b) => string.CompareOrdinal(a.TypeName, b.TypeName));

            return result;

            bool IsExpr(INamedTypeSymbol symbol)
            {
                if (symbol.Name == "IExpr")
                {
                    return true;
                }
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
        public NodeModel(string typeName, bool isSingleton, bool isCustomTraversal, IReadOnlyList<SubNodeModel> subNodes, IReadOnlyList<SubNodeModel> properties)
        {
            this.TypeName = typeName;
            this.IsSingleton = isSingleton;
            this.IsCustomTraversal = isCustomTraversal;
            this.SubNodes = subNodes;
            this.Properties = properties;
        }

        public string TypeName { get; }

        public bool IsSingleton { get; }

        public bool IsCustomTraversal { get; }

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
