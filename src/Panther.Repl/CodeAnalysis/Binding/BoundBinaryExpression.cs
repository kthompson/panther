using System;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public BoundBinaryExpression(BoundExpression left, BoundBinaryOperator @operator, BoundExpression right)
        {
            Operator = @operator;
            Left = left;
            Right = right;
        }

        public BoundExpression Left { get; }

        public BoundBinaryOperator Operator { get; }

        public BoundExpression Right { get; }

        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
        public override Type Type => Left.Type;
    }
}