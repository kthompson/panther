using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Typing;

sealed record TypedMethodExpression(
    SyntaxNode Syntax,
    string Name,
    TypedExpression? Expression,
    ImmutableArray<Symbol> Methods
) : TypedNode(Syntax)
{
    public override TypedNodeKind Kind => TypedNodeKind.MethodExpression;

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitMethodExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitMethodExpression(this);
}
