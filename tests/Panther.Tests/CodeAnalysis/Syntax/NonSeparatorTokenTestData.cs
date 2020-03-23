using Panther.CodeAnalysis.Syntax;

namespace Panther.Tests.CodeAnalysis.Syntax
{
    public class NonSeparatorTokenTestData : TokenTestData
    {
        public NonSeparatorTokenTestData(SyntaxKind kind, string text)
            : base(kind, text)
        {
        }
    }
}