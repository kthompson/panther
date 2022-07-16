using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

sealed record BoundLabelStatement : BoundStatement
{
    public BoundLabel BoundLabel { get; }

    public BoundLabelStatement(SyntaxNode syntax, BoundLabel boundLabel) : base(syntax)
    {
        BoundLabel = boundLabel;
    }

    public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;

    public override void Accept(BoundNodeVisitor visitor) => visitor.VisitLabelStatement(this);

    public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) =>
        visitor.VisitLabelStatement(this);
}
