using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

internal record BoundConversionExpression(SyntaxNode Syntax, Type Type, BoundExpression Expression)
    : BoundExpression(Syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.ConversionExpression;

    public override void Accept(BoundNodeVisitor visitor) =>
        visitor.VisitConversionExpression(this);

    public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) =>
        visitor.VisitConversionExpression(this);
}
