using Panther.CodeAnalysis.Syntax;

namespace Panther.Tests.CodeAnalysis.Syntax;

public class UnaryOperatorSyntaxKind
{
    public SyntaxKind Kind { get; }

    public UnaryOperatorSyntaxKind(SyntaxKind kind)
    {
        Kind = kind;
    }

    public override string ToString() => $"UnaryOperator {Kind.ToString()}";
}
