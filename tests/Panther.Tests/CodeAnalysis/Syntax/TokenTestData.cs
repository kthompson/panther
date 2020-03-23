using Panther.CodeAnalysis.Syntax;

namespace Panther.Tests.CodeAnalysis.Syntax
{
    public class TokenTestData
    {
        public SyntaxKind Kind { get; }
        public string Text { get; }

        public TokenTestData(SyntaxKind kind, string text)
        {
            Kind = kind;
            Text = text;
        }
    }
}