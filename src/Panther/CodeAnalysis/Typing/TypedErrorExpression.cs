using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Typing;

internal sealed record TypedErrorExpression : TypedExpression
{
    public TypedErrorExpression(SyntaxNode syntax)
        : base(syntax) { }

    public override TypedNodeKind Kind => TypedNodeKind.ErrorExpression;

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitErrorExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitErrorExpression(this);

    public override Type Type { get; init; } = Type.Error;
}
