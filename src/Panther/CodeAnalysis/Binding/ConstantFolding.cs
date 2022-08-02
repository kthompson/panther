using System;
using Panther.CodeAnalysis.Symbols;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Binding;

internal class ConstantFolding : TypedNodeVisitor<TypedConstant?>
{
    private ConstantFolding() { }

    public static TypedConstant? Fold(TypedExpression expression)
    {
        var folding = new ConstantFolding();
        return expression.Accept(folding);
    }

    public override TypedConstant? VisitBinaryExpression(TypedBinaryExpression node)
    {
        var left = node.Left;
        var right = node.Right;
        var @operator = node.Operator;

        var leftConstant = left.Accept(this);
        var rightConstant = right.Accept(this);

        if (leftConstant == null)
            return null;

        var leftValue = leftConstant.Value;

        if (@operator.Kind == TypedBinaryOperatorKind.LogicalAnd)
        {
            if (!(bool)leftValue)
                return new TypedConstant(false);

            if (rightConstant != null)
                return new TypedConstant((bool)rightConstant.Value);
        }

        if (@operator.Kind == TypedBinaryOperatorKind.LogicalOr)
        {
            if ((bool)leftValue)
                return new TypedConstant(true);

            if (rightConstant != null)
                return new TypedConstant((bool)rightConstant.Value);
        }

        if (rightConstant == null)
            return null;

        var rightValue = rightConstant.Value;

        switch (@operator.Kind)
        {
            case TypedBinaryOperatorKind.Addition:
                return new TypedConstant(
                    left.Type == Type.Int
                        ? (int)leftValue + (int)rightValue
                        : (string)leftValue + (string)rightValue
                );

            case TypedBinaryOperatorKind.BitwiseAnd:
                return new TypedConstant(
                    left.Type == Type.Int
                        ? (int)leftValue & (int)rightValue
                        : (bool)leftValue & (bool)rightValue
                );

            case TypedBinaryOperatorKind.BitwiseOr:
                return new TypedConstant(
                    left.Type == Type.Int
                        ? (int)leftValue | (int)rightValue
                        : (bool)leftValue | (bool)rightValue
                );

            case TypedBinaryOperatorKind.BitwiseXor:
                return new TypedConstant(
                    left.Type == Type.Int
                        ? (int)leftValue ^ (int)rightValue
                        : (bool)leftValue ^ (bool)rightValue
                );

            case TypedBinaryOperatorKind.Division:
                return new TypedConstant((int)leftValue / (int)rightValue);

            case TypedBinaryOperatorKind.Equal:
                return new TypedConstant(Equals(leftValue, rightValue));

            case TypedBinaryOperatorKind.GreaterThan:
                return new TypedConstant((int)leftValue > (int)rightValue);

            case TypedBinaryOperatorKind.GreaterThanOrEqual:
                return new TypedConstant((int)leftValue >= (int)rightValue);

            case TypedBinaryOperatorKind.LessThan:
                return new TypedConstant((int)leftValue < (int)rightValue);

            case TypedBinaryOperatorKind.LessThanOrEqual:
                return new TypedConstant((int)leftValue <= (int)rightValue);

            case TypedBinaryOperatorKind.Multiplication:
                return new TypedConstant((int)leftValue * (int)rightValue);

            case TypedBinaryOperatorKind.NotEqual:
                return new TypedConstant(!Equals(leftValue, rightValue));

            case TypedBinaryOperatorKind.Subtraction:
                return new TypedConstant((int)leftValue - (int)rightValue);

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override TypedConstant? VisitUnaryExpression(TypedUnaryExpression node)
    {
        var @operator = node.Operator;
        var operandConstant = node.Operand.Accept(this);

        if (operandConstant == null)
            return null;

        var constant = operandConstant.Value;

        return @operator.Kind switch
        {
            TypedUnaryOperatorKind.Identity => operandConstant,
            TypedUnaryOperatorKind.Negation => new TypedConstant(-(int)constant),
            TypedUnaryOperatorKind.LogicalNegation => new TypedConstant(!(bool)constant),
            TypedUnaryOperatorKind.BitwiseNegation => new TypedConstant(~(int)constant),
            _
                => throw new ArgumentOutOfRangeException(
                    nameof(node),
                    $"Unknown operator kind '{@operator.Kind}'"
                )
        };
    }

    public override TypedConstant? VisitIfExpression(TypedIfExpression node)
    {
        var condition = node.Condition.Accept(this);
        if (condition == null)
            return null;

        var clause = (bool)condition.Value ? node.Then : node.Else;
        return clause.Accept(this);
    }

    public override TypedConstant? VisitLiteralExpression(TypedLiteralExpression node) =>
        new(node.Value);
}
