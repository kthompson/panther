using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Typing;

internal sealed record TypedNopStatement : TypedStatement
{
    public override TypedNodeKind Kind => TypedNodeKind.NopStatement;

    public TypedNopStatement(SyntaxNode syntax)
        : base(syntax) { }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitNopStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitNopStatement(this);
}
