using System.Linq;
using FsCheck.Xunit;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Syntax
{
    public class SyntaxFactsTests
    {
        [Property]
        public void SyntaxFactGetTextRoundtrips(SyntaxKind kind)
        {
            var text = SyntaxFacts.GetText(kind);

            if (text == null)
                return;

            var tokens = SyntaxTree.ParseTokens(text);
            var token = Assert.Single(tokens);

            Assert.NotNull(token);
            Assert.Equal(kind, token.Kind);
            Assert.Equal(text, token.Text);
        }
    }
}