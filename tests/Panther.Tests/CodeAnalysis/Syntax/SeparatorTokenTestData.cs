using Panther.CodeAnalysis.Syntax;

namespace Panther.Tests.CodeAnalysis.Syntax;

public class SeparatorTokenTestData : TokenTestData
{
    public SeparatorTokenTestData(SyntaxKind kind, string text) : base(kind, text) { }

    public override string ToString() => $"{Kind} - '{Text}'";
}
