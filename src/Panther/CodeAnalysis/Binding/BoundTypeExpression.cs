using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

/// <summary>
/// Access a type in scope
/// </summary>
internal record BoundTypeExpression(SyntaxNode Syntax, Type Type) : BoundExpression(Syntax)
{
    public override BoundNodeKind Kind => BoundNodeKind.TypeExpression;

    public override void Accept(BoundNodeVisitor visitor) => visitor.VisitTypeExpression(this);

    public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) =>
        visitor.VisitTypeExpression(this);
}
