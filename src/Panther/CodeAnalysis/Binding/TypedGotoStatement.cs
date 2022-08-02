using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

sealed record TypedGotoStatement : TypedStatement
{
    public TypedLabel TypedLabel { get; }

    public override TypedNodeKind Kind => TypedNodeKind.GotoStatement;

    public TypedGotoStatement(SyntaxNode syntax, TypedLabel boundLabel) : base(syntax)
    {
        TypedLabel = boundLabel;
    }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitGotoStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitGotoStatement(this);
}
