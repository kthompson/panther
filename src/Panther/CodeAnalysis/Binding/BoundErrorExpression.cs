using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

internal sealed record BoundErrorExpression : BoundExpression
{
    public BoundErrorExpression(SyntaxNode syntax)
        : base(syntax)
    {
    }

    public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;
    public override void Accept(BoundNodeVisitor visitor) => visitor.VisitErrorExpression(this);

    public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitErrorExpression(this);

    public override Type Type { get ; init; } = Type.Error;
}