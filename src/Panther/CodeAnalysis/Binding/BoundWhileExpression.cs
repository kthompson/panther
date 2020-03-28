using System;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundWhileExpression : BoundExpression
    {
        public BoundExpression Condition { get; }
        public BoundExpression Body { get; }

        public BoundWhileExpression(BoundExpression condition, BoundExpression body)
        {
            Condition = condition;
            Body = body;
        }

        public override BoundNodeKind Kind => BoundNodeKind.WhileExpression;
        public override Type Type => Body.Type;
    }
}