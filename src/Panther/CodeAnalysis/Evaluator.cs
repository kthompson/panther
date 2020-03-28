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
        private readonly BoundBlockExpression _root;
        private readonly Dictionary<VariableSymbol, object> _variables;
        private object? _lastValue;
        private readonly Dictionary<LabelSymbol, int> _labels = new Dictionary<LabelSymbol, int>();

        public Evaluator(BoundBlockExpression root, Dictionary<VariableSymbol, object> variables)
        {
            _root = root;
            _variables = variables;
        }

        private void InitializeLabelLookup()
        {
            for (int i = 0; i < _root.Statements.Length; i++)
            {
                if (_root.Statements[i] is BoundLabelStatement labelStatement)
                {
                    _labels[labelStatement.Label] = i + 1; // skip label
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
                        position = _labels[boundGotoStatement.Label];
                        continue;
                    
                    case BoundLabelStatement _:
                        // noop
                        break;
                    
                    case BoundConditionalGotoStatement conditionalGotoStatement:
                        var cond = (bool)EvaluateExpression(conditionalGotoStatement.Condition);
                        if (conditionalGotoStatement.JumpIfFalse != cond)
                        {
                            position = _labels[conditionalGotoStatement.Label];
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

        private object EvaluateExpression(BoundExpression node)
        {
            switch (node)
            {
                case BoundLiteralExpression n:
                    return n.Value;
                case BoundUnitExpression _:
                    return Unit.Default;
                case BoundAssignmentExpression assignmentStatement:
                {
                    var value = EvaluateExpression(assignmentStatement.Expression);
                    _variables[assignmentStatement.Variable] = value;
                    return Unit.Default;
                }
                case BoundVariableExpression v:
                    return _variables[v.Variable];
                case BoundBinaryExpression binaryExpression:
                {
                    var left = EvaluateExpression(binaryExpression.Left);
                    var right = EvaluateExpression(binaryExpression.Right);

                    return binaryExpression.Operator.Kind switch
                    {
                        BoundBinaryOperatorKind.Addition => (object) ((int) left + (int) right),
                        BoundBinaryOperatorKind.Subtraction => ((int) left - (int) right),
                        BoundBinaryOperatorKind.Multiplication => ((int) left * (int) right),
                        BoundBinaryOperatorKind.Division => ((int) left / (int) right),
                        BoundBinaryOperatorKind.BitwiseAnd => ((int) left & (int) right),
                        BoundBinaryOperatorKind.BitwiseOr => ((int) left | (int) right),
                        BoundBinaryOperatorKind.BitwiseXor => ((int) left ^ (int) right),
                        BoundBinaryOperatorKind.LogicalAnd => ((bool) left && (bool) right),
                        BoundBinaryOperatorKind.LogicalOr => ((bool) left || (bool) right),
                        BoundBinaryOperatorKind.Equal => Equals(left, right),
                        BoundBinaryOperatorKind.NotEqual => !Equals(left, right),
                        BoundBinaryOperatorKind.LessThan => ((int) left < (int) right),
                        BoundBinaryOperatorKind.LessThanOrEqual => ((int) left <= (int) right),
                        BoundBinaryOperatorKind.GreaterThan => ((int) left > (int) right),
                        BoundBinaryOperatorKind.GreaterThanOrEqual => ((int) left >= (int) right),
                        _ => throw new Exception($"Unexpected binary operator {binaryExpression.Operator}")
                    };
                }
                case BoundUnaryExpression unary:
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
                default:
                    throw new Exception($"Unexpected expression {node.Kind}");
            }
        }
    }
}