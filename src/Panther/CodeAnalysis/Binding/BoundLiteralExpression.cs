using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundLiteralExpression : BoundExpression
    {
        public BoundLiteralExpression(SyntaxNode syntax, object value) : base(syntax)
        {
            Type = value switch
            {
                int _ => Type.Int,
                bool _ => Type.Bool,
                string _ => Type.String,
                _ => throw new System.Exception($"Unexpected literal '{value}' of type {value.GetType()}"),
            };

            ConstantValue = new BoundConstant(value);
        }

        public object Value => ConstantValue.Value;

        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override Type Type { get ; init; }
        public override BoundConstant ConstantValue { get; }
    }
}