using Microsoft.CodeAnalysis;

namespace SqExpress.Analyzers
{
    [Generator]
    public sealed partial class TableDescriptorSourceGenerator : IIncrementalGenerator
    {
        private const string TableDescriptorAttributeName = "SqExpress.TableDecalationAttributes.TableDescriptorAttribute";
        private const string ColumnAttributeBaseName = "SqExpress.TableDecalationAttributes.TableColumnAttributeBase";
        private const string IndexAttributeName = "SqExpress.TableDecalationAttributes.IndexAttribute";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
                    fullyQualifiedMetadataName: TableDescriptorAttributeName,
                    predicate: static (node, _) => node is Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax,
                    transform: static (ctx, _) => CreateCandidate(ctx))
                .Where(static candidate => candidate != null)
                .Collect();

            context.RegisterSourceOutput(candidates, static (spc, source) => Execute(spc, source!));
        }
    }
}
