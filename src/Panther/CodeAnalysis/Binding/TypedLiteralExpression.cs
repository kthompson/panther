using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

internal sealed record TypedLiteralExpression : TypedExpression
{
    public TypedLiteralExpression(SyntaxNode syntax, object value) : base(syntax)
    {
        Value = value;
        Type = value switch
        {
            int _ => Type.Int,
            char _ => Type.Char,
            bool _ => Type.Bool,
            string _ => Type.String,
            _
                => throw new System.Exception(
                    $"Unexpected literal '{value}' of type {value.GetType()}"
                ),
        };
    }

    public object Value { get; }
    public override TypedNodeKind Kind => TypedNodeKind.LiteralExpression;
    public override Type Type { get; init; }

    public override void Accept(TypedNodeVisitor visitor) => visitor.VisitLiteralExpression(this);

    public override TResult Accept<TResult>(TypedNodeVisitor<TResult> visitor) =>
        visitor.VisitLiteralExpression(this);
}
