using System;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundForExpression : BoundExpression
    {
        public VariableSymbol Variable { get; }
        public BoundExpression LowerBound { get; }
        public BoundExpression UpperBound { get; }
        public BoundExpression Body { get; }

        public BoundForExpression(VariableSymbol variable, BoundExpression lowerBound, BoundExpression upperBound, BoundExpression body)
        {
            Variable = variable;
            LowerBound = lowerBound;
            UpperBound = upperBound;
            Body = body;
        }

        public override BoundNodeKind Kind => BoundNodeKind.ForExpression;
        public override TypeSymbol Type => TypeSymbol.Unit;
    }
}