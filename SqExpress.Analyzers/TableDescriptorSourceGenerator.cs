using Microsoft.CodeAnalysis;

namespace SqExpress.Analyzers
{
    [Generator]
    public sealed partial class TableDescriptorSourceGenerator : IIncrementalGenerator
    {
        private const string TableDescriptorAttributeName = "SqExpress.TableDecalationAttributes.TableDescriptorAttribute";
        private const string TempTableDescriptorAttributeName = "SqExpress.TableDecalationAttributes.TempTableDescriptorAttribute";
        private const string ColumnAttributeBaseName = "SqExpress.TableDecalationAttributes.TableColumnAttributeBase";
        private const string IndexAttributeName = "SqExpress.TableDecalationAttributes.IndexAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                    predicate: static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax c && c.AttributeLists.Count > 0,
                    transform: static (ctx, _) => CreateCandidate(ctx))
                .Where(static candidate => candidate != null)
                .Collect();

            var options = context.AnalyzerConfigOptionsProvider.Select(static (provider, _) => CreateGeneratorOptions(provider));
            var compilationAndCandidates = context.CompilationProvider.Combine(candidates);
            context.RegisterSourceOutput(compilationAndCandidates.Combine(options), static (spc, source) => Execute(spc, source.Left.Left, source.Left.Right!, source.Right));
        }
    }
}
