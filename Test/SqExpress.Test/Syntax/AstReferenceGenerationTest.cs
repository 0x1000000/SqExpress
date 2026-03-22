#if NET
using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using SqExpress.GenSyntaxTraversal;

namespace SqExpress.Test.Syntax
{
    [TestFixture]
    public class AstReferenceGenerationTest
    {
        [Test]
        public void BuildDocumentationModel_WhenUsingCurrentSyntax_FindsAbstractBases()
        {
            var projectDir = GetSqExpressProjectDirectory();

            var model = Program.BuildModelRoslyn(projectDir);

            Assert.That(model.Any(static m => m.TypeName == "ExprValue" && m.IsAbstract));
            Assert.That(model.Any(static m => m.TypeName == "ExprLike" && m.BaseTypeName == "ExprPredicate"));
            Assert.That(model.Any(static m => m.TypeName == "IExprSelecting" && m.IsInterface && m.BaseTypeName == "IExpr"));
            Assert.That(model.Any(static m => m.TypeName == "ExprAggregateFunction" && m.BaseTypeName == "IExprSelecting"));
        }

        [Test]
        public void GenerateAstReferenceMarkdown_WhenUsingCurrentSyntax_ProducesHierarchyAndLinks()
        {
            var projectDir = GetSqExpressProjectDirectory();
            var model = Program.BuildModelRoslyn(projectDir);
            var builder = new StringBuilder();

            Program.GenerateAstReferenceMarkdown(model, builder);
            var markdown = builder.ToString();

            Assert.That(markdown, Does.Contain("## Hierarchy"));
            Assert.That(markdown, Does.Contain("- [IExpr](#iexpr)"));
            Assert.That(markdown, Does.Contain("- [IExprSelecting](#iexprselecting) _(interface)_"));
            Assert.That(markdown, Does.Not.Contain("### IExprSelecting\r\n\r\n- Kind: interface, singleton"));
            Assert.That(markdown, Does.Not.Contain("### IExprComplete\r\n\r\n- Kind: interface, singleton"));
            Assert.That(markdown, Does.Not.Contain("### ExprBoolean\r\n\r\n- Kind: abstract class, singleton"));
            Assert.That(markdown, Does.Contain("[ExprValue](#exprvalue)"));
            Assert.That(markdown, Does.Contain("### IExprSelecting"));
            Assert.That(markdown, Does.Contain("- Base: [IExpr](#iexpr)"));
            Assert.That(markdown, Does.Contain("### ExprAggregateFunction"));
            Assert.That(markdown, Does.Contain("- Base: [IExprSelecting](#iexprselecting)"));
            Assert.That(markdown, Does.Contain("### ExprLike"));
            Assert.That(markdown, Does.Contain("- Base: [ExprPredicate](#exprpredicate)"));
            Assert.That(markdown, Does.Contain("`Test`: [ExprValue](#exprvalue)"));
            Assert.That(markdown, Does.Contain("`Pattern`: [ExprValue](#exprvalue)"));
        }

        [Test]
        public void ReplaceGeneratedRegion_WhenUsingMarkdownMarkers_PreservesWrapperText()
        {
            const string content = """
                # Title

                Intro

                <!-- CodeGenStart -->
                old
                <!-- CodeGenEnd -->

                Outro
                """;

            var updated = Program.ReplaceGeneratedRegion(content, "new line\n", "<!-- CodeGenStart -->", "<!-- CodeGenEnd -->");

            Assert.That(updated, Does.Contain("# Title"));
            Assert.That(updated, Does.Contain("Intro"));
            Assert.That(updated, Does.Contain("new line"));
            Assert.That(updated, Does.Contain("<!-- CodeGenStart -->"));
            Assert.That(updated, Does.Contain("<!-- CodeGenEnd -->"));
            Assert.That(updated, Does.Contain("Outro"));
            Assert.That(updated, Does.Not.Contain("\nold\n"));
        }

        private static string GetSqExpressProjectDirectory()
            => Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "SqExpress"));
    }
}
#endif
