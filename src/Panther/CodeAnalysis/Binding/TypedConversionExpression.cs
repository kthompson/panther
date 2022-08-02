using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

internal record TypedConversionExpression(SyntaxNode Syntax, Type Type, TypedExpression Expression)
    : TypedExpression(Syntax)
{
    public override TypedNodeKind Kind => TypedNodeKind.ConversionExpression;

    public override void Accept(TypedNodeVisitor visitor) =>
        visitor.VisitConversionExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitConversionExpression(this);
}
