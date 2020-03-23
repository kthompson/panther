using System;
using System.Collections.Generic;
using System.Text;
using Panther.CodeAnalysis.Binding;

namespace Panther.CodeAnalysis
{
    internal class Evaluator
    {
        private readonly BoundExpression _root;
        private readonly Dictionary<VariableSymbol, object> _variables;

        public Evaluator(BoundExpression root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
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

            if (node is BoundVariableExpression v)
            {
                return _variables[v.Variable];
            }

            if (node is BoundAssignmentExpression a)
            {
                var value = EvaluateExpression(a.Expression);
                _variables[a.Variable] = value;
                return value;
            }

            if (node is BoundBinaryExpression binaryExpression)
            {
                var left = EvaluateExpression(binaryExpression.Left);
                var right = EvaluateExpression(binaryExpression.Right);

                return binaryExpression.Operator.Kind switch
                {
                    BoundBinaryOperatorKind.Addition => (object)((int)left + (int)right),
                    BoundBinaryOperatorKind.Subtraction => ((int)left - (int)right),
                    BoundBinaryOperatorKind.Multiplication => ((int)left * (int)right),
                    BoundBinaryOperatorKind.Division => ((int)left / (int)right),
                    BoundBinaryOperatorKind.LogicalAnd => ((bool)left && (bool)right),
                    BoundBinaryOperatorKind.LogicalOr => ((bool)left || (bool)right),
                    BoundBinaryOperatorKind.Equal => Equals(left, right),
                    BoundBinaryOperatorKind.NotEqual => !Equals(left, right),
                    _ => throw new Exception($"Unexpected binary operator {binaryExpression.Operator}")
                };
            }

            if (node is BoundUnaryExpression unary)
            {
                var operand = EvaluateExpression(unary.Operand);
                return unary.Operator.Kind switch
                {
                    BoundUnaryOperatorKind.Negation => (object)-(int)operand,
                    BoundUnaryOperatorKind.Identity => (int)operand,
                    BoundUnaryOperatorKind.LogicalNegation => !(bool)operand,
                    _ => throw new Exception($"Unexpected unary operator {unary.Operator}")
                };
            }

            throw new Exception($"Unexpected expression {node.Kind}");
        }
    }
}