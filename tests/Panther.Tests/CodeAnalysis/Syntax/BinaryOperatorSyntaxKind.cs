using Panther.CodeAnalysis.Syntax;

namespace Panther.Tests.CodeAnalysis.Syntax;

public class BinaryOperatorSyntaxKind
{
    public SyntaxKind Kind { get; }

    public BinaryOperatorSyntaxKind(SyntaxKind kind)
    {
        Kind = kind;
    }

    public override string ToString() => $"BinaryOperator {Kind.ToString()}";
}
