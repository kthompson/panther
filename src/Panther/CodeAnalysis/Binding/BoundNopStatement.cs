using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

internal sealed record BoundNopStatement : BoundStatement
{
    public override BoundNodeKind Kind => BoundNodeKind.NopStatement;

    public BoundNopStatement(SyntaxNode syntax) : base(syntax)
    {
    }

    public override void Accept(BoundNodeVisitor visitor) => visitor.VisitNopStatement(this);
    public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitNopStatement(this);
}