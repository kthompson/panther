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

            if (node is BoundAssignmentExpression assignmentStatement)
            {
                var value = EvaluateExpression(assignmentStatement.Expression);
                _variables[assignmentStatement.Variable] = value;
                _lastValue = Unit.Default;
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

                switch (binaryExpression.Operator.Kind)
                {
                    case BoundBinaryOperatorKind.Addition:
                        return (object)((int)left + (int)right);

                    case BoundBinaryOperatorKind.Subtraction:
                        return ((int)left - (int)right);

                    case BoundBinaryOperatorKind.Multiplication:
                        return ((int)left * (int)right);

                    case BoundBinaryOperatorKind.Division:
                        return ((int)left / (int)right);

                    case BoundBinaryOperatorKind.BitwiseAnd:
                        return ((int)left & (int)right);

                    case BoundBinaryOperatorKind.BitwiseOr:
                        return ((int)left | (int)right);

                    case BoundBinaryOperatorKind.BitwiseXor:
                        return ((int)left ^ (int)right);

                    case BoundBinaryOperatorKind.LogicalAnd:
                        return ((bool)left && (bool)right);

                    case BoundBinaryOperatorKind.LogicalOr:
                        return ((bool)left || (bool)right);

                    case BoundBinaryOperatorKind.Equal:
                        return Equals(left, right);

                    case BoundBinaryOperatorKind.NotEqual:
                        return !Equals(left, right);

                    case BoundBinaryOperatorKind.LessThan:
                        return (int)left < (int)right;

                    case BoundBinaryOperatorKind.LessThanOrEqual:
                        return (int)left <= (int)right;

                    case BoundBinaryOperatorKind.GreaterThan:
                        return (int)left > (int)right;

                    case BoundBinaryOperatorKind.GreaterThanOrEqual:
                        return (int)left >= (int)right;

                    default:
                        throw new Exception($"Unexpected binary operator {binaryExpression.Operator}");
                }
            }

            if (node is BoundUnaryExpression unary)
            {
                var operand = EvaluateExpression(unary.Operand);
                return unary.Operator.Kind switch
                {
                    BoundUnaryOperatorKind.Negation => (object)-(int)operand,
                    BoundUnaryOperatorKind.Identity => (int)operand,
                    BoundUnaryOperatorKind.LogicalNegation => !(bool)operand,
                    BoundUnaryOperatorKind.BitwiseNegation => ~(int)operand,
                    _ => throw new Exception($"Unexpected unary operator {unary.Operator}")
                };
            }

            if (node is BoundIfExpression ifExpression)
            {
                var cond = (bool)EvaluateExpression(ifExpression.Condition);
                return EvaluateExpression(cond ? ifExpression.Then : ifExpression.Else);
            }

            if (node is BoundWhileExpression whileExpression)
            {
                while (true)
                {
                    var cond = (bool)EvaluateExpression(whileExpression.Condition);
                    if (!cond)
                        break;

                    EvaluateExpression(whileExpression.Expression);
                }

                return Unit.Default;
            }

            if (node is BoundForExpression forExpression)
            {
                var lower = (int)EvaluateExpression(forExpression.LowerBound);
                var upper = (int)EvaluateExpression(forExpression.UpperBound);
                for (int i = lower; i < upper; i++)
                {
                    _variables[forExpression.Variable] = i;
                    EvaluateExpression(forExpression.Body);
                }

                return Unit.Default;
            }

            throw new Exception($"Unexpected expression {node.Kind}");
        }
    }
}