﻿using System;
using System.Text;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis
{
    internal class Evaluator
    {
        private readonly BoundExpression _root;

        public Evaluator(BoundExpression root)
        {
            _root = root;
        }

        public object Evaluate()
        {
            return EvaluateExpression(_root);
        }

        private object EvaluateExpression(BoundExpression node)
        {
            if (node is BoundLiteralExpression n)
            {
                return n.Value;
            }

            if (node is BoundBinaryExpression binaryExpression)
            {
                var left = EvaluateExpression(binaryExpression.Left);
                var right = EvaluateExpression(binaryExpression.Right);

                switch (binaryExpression.Operator.Kind)
                {
                    case BoundBinaryOperatorKind.Addition:
                        return (int)left + (int)right;

                    case BoundBinaryOperatorKind.Subtraction:
                        return (int)left - (int)right;

                    case BoundBinaryOperatorKind.Multiplication:
                        return (int)left * (int)right;

                    case BoundBinaryOperatorKind.Division:
                        return (int)left / (int)right;

                    case BoundBinaryOperatorKind.LogicalAnd:
                        return (bool)left && (bool)right;

                    case BoundBinaryOperatorKind.LogicalOr:
                        return (bool)left || (bool)right;

                    case BoundBinaryOperatorKind.Equal:
                        return Equals(left, right);

                    case BoundBinaryOperatorKind.NotEqual:
                        return !Equals(left, right);

                    default:
                        throw new Exception($"Unexpected binary operator {binaryExpression.Operator}");
                }
            }

            if (node is BoundUnaryExpression unary)
            {
                var operand = EvaluateExpression(unary.Operand);
                switch (unary.Operator.Kind)
                {
                    case BoundUnaryOperatorKind.Negation:
                        return -(int)operand;

                    case BoundUnaryOperatorKind.Identity:
                        return (int)operand;

                    case BoundUnaryOperatorKind.LogicalNegation:
                        return !(bool)operand;

                    default:
                        throw new Exception($"Unexpected unary operator {unary.Operator}");
                }
            }

            throw new Exception($"Unexpected expression {node.Kind}");
        }
    }
}