using System;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundWhileExpression : BoundLoopExpression
    {
        public BoundExpression Condition { get; }
        public BoundExpression Body { get; }

        public BoundWhileExpression(BoundExpression condition, BoundExpression body, BoundLabel breakLabel,
            BoundLabel continueLabel)
            : base(breakLabel, continueLabel)
        {
            Condition = condition;
            Body = body;
        }

        public override BoundNodeKind Kind => BoundNodeKind.WhileExpression;
        public override TypeSymbol Type => Body.Type;
    }
}