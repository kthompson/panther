using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Typing;

sealed record TypedLabelStatement(SyntaxNode Syntax, TypedLabel TypedLabel) : TypedStatement(Syntax)
{
    public override TypedNodeKind Kind => TypedNodeKind.LabelStatement;

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitLabelStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitLabelStatement(this);
}
