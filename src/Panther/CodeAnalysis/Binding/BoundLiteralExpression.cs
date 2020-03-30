using System;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundLiteralExpression : BoundExpression
    {
        public BoundLiteralExpression(object value)
        {
            Value = value;
            Type = value switch
            {
                int _ => TypeSymbol.Int,
                bool _ => TypeSymbol.Bool,
                string _ => TypeSymbol.String,
                _ => throw new Exception($"Unexpected literal '{value}' of type {value.GetType()}"),
            };
        }

        public object Value { get; }

        public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
        public override TypeSymbol Type { get; }
    }
}