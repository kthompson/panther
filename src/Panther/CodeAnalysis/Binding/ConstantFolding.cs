using System;
using Panther.CodeAnalysis.Symbols;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Binding
{
    internal static class ConstantFolding
    {
        public static BoundConstant? ComputeConstant(BoundUnaryOperator @operator, BoundExpression operand)
        {
            if (operand.ConstantValue == null)
                return null;

            var constant = operand.ConstantValue.Value;

            return @operator.Kind switch
            {
                BoundUnaryOperatorKind.Identity => operand.ConstantValue,
                BoundUnaryOperatorKind.Negation => new BoundConstant(-(int)constant),
                BoundUnaryOperatorKind.LogicalNegation => new BoundConstant(!(bool)constant),
                BoundUnaryOperatorKind.BitwiseNegation => new BoundConstant(~(int)constant),
                _ => throw new ArgumentOutOfRangeException(nameof(@operator),
                    $"Unknown operator kind '{@operator.Kind}'")
            };
        }

        public static BoundConstant? ComputeConstant(BoundExpression left, BoundBinaryOperator @operator, BoundExpression right)
        {
            var leftConstant = left.ConstantValue;
            var rightConstant = right.ConstantValue;

            if (leftConstant == null)
                return null;

            var leftValue = leftConstant.Value;

            if (@operator.Kind == BoundBinaryOperatorKind.LogicalAnd)
            {
                if (!(bool)leftValue)
                    return new BoundConstant(false);

                if (rightConstant != null)
                    return new BoundConstant((bool)rightConstant.Value);
            }

            if (@operator.Kind == BoundBinaryOperatorKind.LogicalOr)
            {
                if ((bool)leftValue)
                    return new BoundConstant(true);

                if (rightConstant != null)
                    return new BoundConstant((bool)rightConstant.Value);
            }

            if (rightConstant == null)
                return null;

            var rightValue = rightConstant.Value;

            switch (@operator.Kind)
            {
                case BoundBinaryOperatorKind.Addition:
                    return new BoundConstant(left.Type == Type.Int
                        ? (int)leftValue + (int)rightValue
                        : (object)((string)leftValue + (string)rightValue));

                case BoundBinaryOperatorKind.BitwiseAnd:
                    return new BoundConstant(left.Type == Type.Int
                        ? (int)leftValue & (int)rightValue
                        : (object)((bool)leftValue & (bool)rightValue));

                case BoundBinaryOperatorKind.BitwiseOr:
                    return new BoundConstant(left.Type == Type.Int
                        ? (int)leftValue | (int)rightValue
                        : (object)((bool)leftValue | (bool)rightValue));

                case BoundBinaryOperatorKind.BitwiseXor:
                    return new BoundConstant(left.Type == Type.Int
                        ? (int)leftValue ^ (int)rightValue
                        : (object)((bool)leftValue ^ (bool)rightValue));

                case BoundBinaryOperatorKind.Division:
                    return new BoundConstant((int)leftValue / (int)rightValue);

                case BoundBinaryOperatorKind.Equal:
                    return new BoundConstant(Equals(leftValue, rightValue));

                case BoundBinaryOperatorKind.GreaterThan:
                    return new BoundConstant((int)leftValue > (int)rightValue);

                case BoundBinaryOperatorKind.GreaterThanOrEqual:
                    return new BoundConstant((int)leftValue >= (int)rightValue);

                case BoundBinaryOperatorKind.LessThan:
                    return new BoundConstant((int)leftValue < (int)rightValue);

                case BoundBinaryOperatorKind.LessThanOrEqual:
                    return new BoundConstant((int)leftValue <= (int)rightValue);

                case BoundBinaryOperatorKind.Multiplication:
                    return new BoundConstant((int)leftValue * (int)rightValue);

                case BoundBinaryOperatorKind.NotEqual:
                    return new BoundConstant(!Equals(leftValue, rightValue));

                case BoundBinaryOperatorKind.Subtraction:
                    return new BoundConstant((int)leftValue - (int)rightValue);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}