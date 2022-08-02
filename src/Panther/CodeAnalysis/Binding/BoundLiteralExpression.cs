using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

internal sealed record BoundLiteralExpression : BoundExpression
{
    public BoundLiteralExpression(SyntaxNode syntax, object value) : base(syntax)
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
    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
    public override Type Type { get; init; }

    public override void Accept(BoundNodeVisitor visitor) => visitor.VisitLiteralExpression(this);

    public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) =>
        visitor.VisitLiteralExpression(this);
}
