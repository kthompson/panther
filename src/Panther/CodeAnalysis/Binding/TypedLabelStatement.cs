using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

sealed record TypedLabelStatement : TypedStatement
{
    public TypedLabel TypedLabel { get; }

    public TypedLabelStatement(SyntaxNode syntax, TypedLabel boundLabel) : base(syntax)
    {
        TypedLabel = boundLabel;
    }

    public override TypedNodeKind Kind => TypedNodeKind.LabelStatement;

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitLabelStatement(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitLabelStatement(this);
}
