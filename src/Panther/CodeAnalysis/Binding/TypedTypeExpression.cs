using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

/// <summary>
/// Access a type in scope
/// </summary>
internal record TypedTypeExpression(SyntaxNode Syntax, Type Type) : TypedExpression(Syntax)
{
    public override TypedNodeKind Kind => TypedNodeKind.TypeExpression;

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitTypeExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitTypeExpression(this);
}
