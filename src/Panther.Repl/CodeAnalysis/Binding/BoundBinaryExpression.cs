using System;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public BoundBinaryExpression(BoundExpression left, BoundBinaryOperatorKind operatorKind, BoundExpression right)
        {
            OperatorKind = operatorKind;
            Left = left;
            Right = right;
        }

        public BoundExpression Left { get; }

        public BoundBinaryOperatorKind OperatorKind { get; }

        public BoundExpression Right { get; }

        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
        public override Type Type => Left.Type;
    }
}