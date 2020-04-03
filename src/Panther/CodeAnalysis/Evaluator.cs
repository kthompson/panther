using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis
{
    internal class Evaluator
    {
        private readonly BoundBlockExpression _root;
        private readonly Dictionary<VariableSymbol, object> _variables;
        private readonly IBuiltins _builtins;
        private readonly Random _random = new Random();
        private object? _lastValue;
        private readonly Dictionary<BoundLabel, int> _labels = new Dictionary<BoundLabel, int>();

        public Evaluator(BoundBlockExpression root, Dictionary<VariableSymbol, object> variables, IBuiltins builtins)
        {
            _root = root;
            _variables = variables;
            _builtins = builtins;
        }

        private void InitializeLabelLookup()
        {
            for (int i = 0; i < _root.Statements.Length; i++)
            {
                if (_root.Statements[i] is BoundLabelStatement labelStatement)
                {
                    _labels[labelStatement.BoundLabel] = i + 1; // skip label
                }
            }
        }

        public object? Evaluate()
        {
            InitializeLabelLookup();

            var position = 0;
            while (position < _root.Statements.Length)
            {
                switch (_root.Statements[position])
                {
                    case BoundGotoStatement boundGotoStatement:
                        position = _labels[boundGotoStatement.BoundLabel];
                        continue;

                    case BoundLabelStatement _:
                        // noop
                        break;

                    case BoundConditionalGotoStatement conditionalGotoStatement:
                        var cond = (bool)EvaluateExpression(conditionalGotoStatement.Condition);
                        if (conditionalGotoStatement.JumpIfFalse != cond)
                        {
                            position = _labels[conditionalGotoStatement.BoundLabel];
                            continue;
                        }
                        break;

                    case BoundVariableDeclarationStatement a:
                        {
                            var value = EvaluateExpression(a.Expression);
                            _variables[a.Variable] = value;
                            _lastValue = value;
                            break;
                        }
                    case BoundExpressionStatement expressionStatement:
                        _lastValue = EvaluateExpression(expressionStatement.Expression);
                        break;

                    default:
                        throw new Exception($"Unexpected statement {_root.Statements[position].Kind}");
                }

                position++;
            }

            _lastValue = EvaluateExpression(_root.Expression);

            return _lastValue;
        }

        private object EvaluateExpression(BoundExpression node) =>
            node switch
            {
                BoundLiteralExpression n => n.Value,
                BoundUnitExpression _ => Unit.Default,
                BoundConversionExpression conversionExpression => EvaluateConversionExpression(conversionExpression),
                BoundCallExpression callExpression => EvaluateCallExpressions(callExpression),
                BoundAssignmentExpression assignmentStatement => EvaluateAssignmentExpression(assignmentStatement),
                BoundVariableExpression v => EvaluateVariableExpression(v),
                BoundBinaryExpression binaryExpression => EvaluateBinaryExpression(binaryExpression),
                BoundUnaryExpression unary => EvaluateUnaryExpression(unary),
                _ => throw new Exception($"Unexpected expression {node.Kind}")
            };

        private object EvaluateConversionExpression(BoundConversionExpression conversionExpression)
        {
            var value = EvaluateExpression(conversionExpression.Expression);

            if (conversionExpression.Type == TypeSymbol.String)
            {
                return Convert.ToString(value);
            }

            if (conversionExpression.Type == TypeSymbol.Bool)
            {
                return Convert.ToBoolean(value);
            }

            if (conversionExpression.Type == TypeSymbol.Int)
            {
                return Convert.ToInt32(value);
            }

            throw new Exception($"Unexpected type {conversionExpression.Type}");
        }

        private object EvaluateAssignmentExpression(BoundAssignmentExpression assignmentStatement)
        {
            var value = EvaluateExpression(assignmentStatement.Expression);
            _variables[assignmentStatement.Variable] = value;
            return Unit.Default;
        }

        private object EvaluateVariableExpression(BoundVariableExpression v)
        {
            return _variables[v.Variable];
        }

        private object EvaluateBinaryExpression(BoundBinaryExpression binaryExpression)
        {
            var left = EvaluateExpression(binaryExpression.Left);
            var right = EvaluateExpression(binaryExpression.Right);

            return binaryExpression.Operator.Kind switch
            {
                BoundBinaryOperatorKind.Addition => binaryExpression.Type == TypeSymbol.Int
                    ? (object)((int)left + (int)right)
                    : (object)((string)left + (string)right),
                BoundBinaryOperatorKind.Subtraction => ((int)left - (int)right),
                BoundBinaryOperatorKind.Multiplication => ((int)left * (int)right),
                BoundBinaryOperatorKind.Division => ((int)left / (int)right),
                BoundBinaryOperatorKind.BitwiseAnd => ((int)left & (int)right),
                BoundBinaryOperatorKind.BitwiseOr => ((int)left | (int)right),
                BoundBinaryOperatorKind.BitwiseXor => ((int)left ^ (int)right),
                BoundBinaryOperatorKind.LogicalAnd => ((bool)left && (bool)right),
                BoundBinaryOperatorKind.LogicalOr => ((bool)left || (bool)right),
                BoundBinaryOperatorKind.Equal => Equals(left, right),
                BoundBinaryOperatorKind.NotEqual => !Equals(left, right),
                BoundBinaryOperatorKind.LessThan => ((int)left < (int)right),
                BoundBinaryOperatorKind.LessThanOrEqual => ((int)left <= (int)right),
                BoundBinaryOperatorKind.GreaterThan => ((int)left > (int)right),
                BoundBinaryOperatorKind.GreaterThanOrEqual => ((int)left >= (int)right),
                _ => throw new Exception($"Unexpected binary operator {binaryExpression.Operator}")
            };
        }

        private object EvaluateUnaryExpression(BoundUnaryExpression unary)
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

        private object EvaluateCallExpressions(BoundCallExpression callExpression)
        {
            if (callExpression.Function == BuiltinFunctions.Print)
            {
                var message = (string)EvaluateExpression(callExpression.Arguments[0]);
                _builtins.Print(message);
                return Unit.Default;
            }

            if (callExpression.Function == BuiltinFunctions.Read)
            {
                return _builtins.Read();
            }

            if (callExpression.Function == BuiltinFunctions.Rnd)
            {
                return _random.Next((int)EvaluateExpression(callExpression.Arguments[0]));
            }

            throw new Exception($"Unexpected function {callExpression.Function}");
        }
    }
}