using System;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundWhileExpression : BoundExpression
    {
        public BoundExpression Condition { get; }
        public BoundExpression Expression { get; }

        public BoundWhileExpression(BoundExpression condition, BoundExpression expression)
        {
            Condition = condition;
            Expression = expression;
        }

        public override BoundNodeKind Kind => BoundNodeKind.WhileExpression;
        public override Type Type => Expression.Type;
    }
}