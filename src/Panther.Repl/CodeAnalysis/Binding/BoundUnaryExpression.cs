using System;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public BoundUnaryExpression(BoundUnaryOperator @operator, BoundExpression operand)
        {
            Operator = @operator;
            Operand = operand;
        }

        public BoundUnaryOperator Operator { get; }

        public BoundExpression Operand { get; }

        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override Type Type => Operand.Type;
    }
}