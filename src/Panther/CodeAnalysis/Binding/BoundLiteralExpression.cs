using System;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundLiteralExpression : BoundExpression
    {
        public BoundLiteralExpression(object value)
        {
            Type = value switch
            {
                int _ => TypeSymbol.Int,
                bool _ => TypeSymbol.Bool,
                string _ => TypeSymbol.String,
                _ => throw new Exception($"Unexpected literal '{value}' of type {value.GetType()}"),
            };

            ConstantValue = new BoundConstant(value);
        }

        public object Value => ConstantValue.Value;

        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }
        public override BoundConstant ConstantValue { get; }
    }
}