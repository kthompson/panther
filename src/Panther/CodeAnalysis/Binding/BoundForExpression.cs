using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundForExpression : BoundLoopExpression
    {
        public VariableSymbol Variable { get; }
        public BoundExpression LowerBound { get; }
        public BoundExpression UpperBound { get; }
        public BoundExpression Body { get; }

        public BoundForExpression(SyntaxNode syntax, VariableSymbol variable, BoundExpression lowerBound,
            BoundExpression upperBound,
            BoundExpression body, BoundLabel breakLabel, BoundLabel continueLabel)
            : base(syntax, breakLabel, continueLabel)
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