using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

sealed record BoundGotoStatement : BoundStatement
{
    public BoundLabel BoundLabel { get; }

    public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;

    public BoundGotoStatement(SyntaxNode syntax, BoundLabel boundLabel) : base(syntax)
    {
        BoundLabel = boundLabel;
    }

    public override void Accept(BoundNodeVisitor visitor) => visitor.VisitGotoStatement(this);

    public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) =>
        visitor.VisitGotoStatement(this);
}
