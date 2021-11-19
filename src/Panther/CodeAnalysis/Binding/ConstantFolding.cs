using System;
using Panther.CodeAnalysis.Symbols;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Binding
{
    internal class ConstantFolding: BoundNodeVisitor<BoundConstant?>
    {
        private ConstantFolding()
        {
        }

        public static BoundConstant? Fold(BoundExpression expression)
        {
            var folding = new ConstantFolding();
            return expression.Accept(folding);
        }

        public override BoundConstant? VisitBinaryExpression(BoundBinaryExpression node)
        {
            var left = node.Left;
            var right = node.Right;
            var @operator = node.Operator;

            var leftConstant = left.Accept(this);
            var rightConstant = right.Accept(this);

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
                        : (string)leftValue + (string)rightValue);

                case BoundBinaryOperatorKind.BitwiseAnd:
                    return new BoundConstant(left.Type == Type.Int
                        ? (int)leftValue & (int)rightValue
                        : (bool)leftValue & (bool)rightValue);

                case BoundBinaryOperatorKind.BitwiseOr:
                    return new BoundConstant(left.Type == Type.Int
                        ? (int)leftValue | (int)rightValue
                        : (bool)leftValue | (bool)rightValue);

                case BoundBinaryOperatorKind.BitwiseXor:
                    return new BoundConstant(left.Type == Type.Int
                        ? (int)leftValue ^ (int)rightValue
                        : (bool)leftValue ^ (bool)rightValue);

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

        public override BoundConstant? VisitUnaryExpression(BoundUnaryExpression node)
        {
            var @operator = node.Operator;
            var operandConstant = node.Operand.Accept(this);

            if (operandConstant == null)
                return null;

            var constant = operandConstant.Value;

            return @operator.Kind switch
            {
                BoundUnaryOperatorKind.Identity => operandConstant,
                BoundUnaryOperatorKind.Negation => new BoundConstant(-(int)constant),
                BoundUnaryOperatorKind.LogicalNegation => new BoundConstant(!(bool)constant),
                BoundUnaryOperatorKind.BitwiseNegation => new BoundConstant(~(int)constant),
                _ => throw new ArgumentOutOfRangeException(nameof(node), $"Unknown operator kind '{@operator.Kind}'")
            };
        }

        public override BoundConstant? VisitIfExpression(BoundIfExpression node)
        {
            var condition = node.Condition.Accept(this);
            if (condition == null)
                return null;

            var clause = (bool)condition.Value ? node.Then : node.Else;
            return clause.Accept(this);
        }

        public override BoundConstant? VisitLiteralExpression(BoundLiteralExpression node) => new(node.Value);
    }
}