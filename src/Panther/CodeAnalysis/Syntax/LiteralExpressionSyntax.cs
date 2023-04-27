using System.Collections.Generic;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax;

public sealed partial record LiteralExpressionSyntax(
    SourceFile SourceFile,
    SyntaxToken LiteralToken,
    object? Value
) : ExpressionSyntax(SourceFile)
{
    public override SyntaxKind Kind => SyntaxKind.LiteralExpression;

    public override IEnumerable<SyntaxNode> GetChildren()
    {
        yield return LiteralToken;
    }

    public override void Accept(SyntaxVisitor visitor)
    {
        visitor.VisitLiteralExpression(this);
    }

    public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) =>
        visitor.VisitLiteralExpression(this);

    public LiteralExpressionSyntax(SourceFile sourceFile, SyntaxToken literalToken)
        : this(sourceFile, literalToken, literalToken.Value) { }
}
