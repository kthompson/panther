using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Panther.CodeAnalysis.Binding;

namespace Panther.CodeAnalysis
{
    public class Unit
    {
        private Unit()
        {
        }

        public static readonly Unit Default = new Unit();

        public override string ToString()
        {
            return "unit";
        }
    }

    internal class Evaluator
    {
        private readonly BoundStatement _root;
        private readonly Dictionary<VariableSymbol, object> _variables;
        private object? _lastValue;

        public Evaluator(BoundStatement root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
        }

        public object? Evaluate()
        {
            EvaluateStatement(_root);

            return _lastValue;
        }

        private void EvaluateStatement(BoundStatement node)
        {
            if (node is BoundVariableDeclarationStatement a)
            {
                var value = EvaluateExpression(a.Expression);
                _variables[a.Variable] = value;
                _lastValue = value;
                return;
            }

            if (node is BoundExpressionStatement expressionStatement)
            {
                _lastValue = EvaluateExpression(expressionStatement.Expression);
                return;
            }

            if (node is BoundAssignmentStatement assignmentStatement)
            {
                var value = EvaluateExpression(assignmentStatement.Expression);
                _variables[assignmentStatement.Variable] = value;
                _lastValue = value;
                return;
            }

            throw new Exception($"Unexpected statement {node.Kind}");
        }

        private object EvaluateExpression(BoundExpression node)
        {
            if (node is BoundLiteralExpression n)
            {
                return n.Value;
            }

            if (node is BoundUnitExpression)
            {
                return Unit.Default;
            }

            if (node is BoundVariableExpression v)
            {
                return _variables[v.Variable];
            }

            if (node is BoundBlockExpression block)
            {
                foreach (var statement in block.Statements)
                {
                    EvaluateStatement(statement);
                }

                return EvaluateExpression(block.Expression);
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