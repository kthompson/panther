using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.StdLib;

namespace Panther.CodeAnalysis
{
    internal class Evaluator
    {
        private readonly BoundProgram _program;
        private readonly Dictionary<VariableSymbol, object> _globals;

        private readonly Dictionary<FunctionSymbol, BoundBlockExpression> _functions =
            new Dictionary<FunctionSymbol, BoundBlockExpression>();
        private readonly Stack<Dictionary<VariableSymbol, object>> _locals = new Stack<Dictionary<VariableSymbol, object>>();
        private readonly IBuiltins _builtins;
        private readonly Random _random = new Random();
        private object? _lastValue;

        public Evaluator(BoundProgram program, Dictionary<VariableSymbol, object> globals, IBuiltins builtins)
        {
            _program = program;
            _globals = globals;
            _builtins = builtins;
            _locals.Push(new Dictionary<VariableSymbol, object>());

            BoundProgram? p = program;

            while (p != null)
            {
                foreach (var (key, value) in p.Functions)
                {
                    _functions.Add(key, value);
                }

                p = p.Previous;
            }
        }

        public object? Evaluate()
        {
            var function = _program.MainFunction ?? _program.ScriptFunction;

            if (function == null)
                return null;

            var body = _functions[function];
            return EvaluateBlockExpression(body);
        }

        private object EvaluateBlockExpression(BoundBlockExpression body)
        {
            var labels = new Dictionary<BoundLabel, int>();

            for (int i = 0; i < body.Statements.Length; i++)
            {
                if (body.Statements[i] is BoundLabelStatement labelStatement)
                {
                    labels[labelStatement.BoundLabel] = i + 1; // skip label
                }
            }

            var position = 0;
            while (position < body.Statements.Length)
            {
                switch (body.Statements[position])
                {
                    case BoundGotoStatement boundGotoStatement:
                        position = labels[boundGotoStatement.BoundLabel];
                        continue;

                    case BoundLabelStatement _:
                        // noop
                        break;

                    case BoundConditionalGotoStatement conditionalGotoStatement:
                        var cond = (bool)EvaluateExpression(conditionalGotoStatement.Condition);
                        if (conditionalGotoStatement.JumpIfTrue == cond)
                        {
                            position = labels[conditionalGotoStatement.BoundLabel];
                            continue;
                        }

                        break;

                    case BoundVariableDeclarationStatement a:
                    {
                        _lastValue = EvaluateVariableDeclaration(a);
                        break;
                    }

                    case BoundAssignmentStatement a:
                    {
                        EvaluateAssignmentStatement(a);
                        break;
                    }
                    case BoundExpressionStatement expressionStatement:
                        _lastValue = EvaluateExpression(expressionStatement.Expression);
                        break;

                    default:
                        throw new Exception($"Unexpected statement {body.Statements[position].Kind}");
                }

                position++;
            }

            _lastValue = EvaluateExpression(body.Expression);

            return _lastValue;
        }

        private void EvaluateAssignmentStatement(BoundAssignmentStatement a)
        {
            var value = EvaluateExpression(a.Expression);
            Assign(a.Variable, value);
            _lastValue = Unit.Default;
        }

        private object EvaluateVariableDeclaration(BoundVariableDeclarationStatement a)
        {
            var value = EvaluateExpression(a.Expression);
            Assign(a.Variable, value);
            return value;
        }

        private void Assign(VariableSymbol variableSymbol, object value)
        {
            if (variableSymbol.Kind == SymbolKind.GlobalVariable)
            {
                _globals[variableSymbol] = value;
            }
            else
            {
                var locals = _locals.Peek();
                locals[variableSymbol] = value;
            }
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
                BoundBlockExpression block => EvaluateBlockExpression(block),
                _ => throw new Exception($"Unexpected expression {node.Kind}")
            };

        private object EvaluateConversionExpression(BoundConversionExpression conversionExpression)
        {
            var value = EvaluateExpression(conversionExpression.Expression);

            if (conversionExpression.Type == TypeSymbol.Any)
                return value;

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
            Assign(assignmentStatement.Variable, value);
            return Unit.Default;
        }

        private object EvaluateVariableExpression(BoundVariableExpression v)
        {
            if (v.Variable.Kind == SymbolKind.GlobalVariable)
                return _globals[v.Variable];

            var locals = _locals.Peek();
            return locals[v.Variable];
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

        private object EvaluateCallExpressions(BoundCallExpression node)
        {
            if (node.Function == BuiltinFunctions.Print)
            {
                var message = (string)EvaluateExpression(node.Arguments[0]);
                _builtins.Print(message);
                return Unit.Default;
            }

            if (node.Function == BuiltinFunctions.Read)
            {
                return _builtins.Read();
            }

            if (node.Function == BuiltinFunctions.Rnd)
            {
                return _random.Next((int)EvaluateExpression(node.Arguments[0]));
            }

            var locals = new Dictionary<VariableSymbol, object>();
            for (int i = 0; i < node.Arguments.Length; i++)
            {
                var parameter = node.Function.Parameters[i];
                var value = EvaluateExpression(node.Arguments[i]);
                locals.Add(parameter, value);
            }

            _locals.Push(locals);

            var expression = _functions[node.Function];
            var result = EvaluateExpression(expression);

            _locals.Pop();

            return result;
        }
    }
}