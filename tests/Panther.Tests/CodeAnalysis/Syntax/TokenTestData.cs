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

        public override string ToString()
        {
            return $"{Kind}: {Text}";
        }
    }
}