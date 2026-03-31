using Microsoft.CodeAnalysis;
using SqExpress.TableDecalationAttributes;

namespace SqExpress.Analyzers
{
    [Generator]
    public sealed partial class TableDescriptorSourceGenerator : IIncrementalGenerator
    {
        private static readonly string TableDescriptorAttributeName = typeof(TableDescriptorAttribute).FullName!;
        private static readonly string TempTableDescriptorAttributeName = typeof(TempTableDescriptorAttribute).FullName!;
        private static readonly string DerivedTableDescriptorAttributeName = typeof(DerivedTableDescriptorAttribute).FullName!;
        private static readonly string ColumnAttributeBaseName = typeof(TableColumnAttributeBase).FullName!;
        private static readonly string DerivedColumnAttributeBaseName = typeof(DerivedColumnAttributeBase).FullName!;
        private static readonly string IndexAttributeName = typeof(IndexAttribute).FullName!;

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
