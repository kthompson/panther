using System;
using System.Collections.Generic;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class Binder
    {
        private readonly List<string> _diagnostics = new List<string>();

        public IEnumerable<string> Diagnostics => _diagnostics;

        public BoundExpression BindExpression(ExpressionSyntax syntax)
        {
            switch (syntax.Kind)
            {
                case SyntaxKind.LiteralExpression:
                    return BindLiteralExpression((LiteralExpressionSyntax)syntax);

                case SyntaxKind.BinaryExpression:
                    return BindBinaryExpression((BinaryExpressionSyntax)syntax);

                case SyntaxKind.UnaryExpression:
                    return BindUnaryExpression((UnaryExpressionSyntax)syntax);

                default:
                    throw new Exception($"Unexpected syntax {syntax.Kind}");
            }
        }

        private BoundExpression BindUnaryExpression(UnaryExpressionSyntax syntax)
        {
            //var value = syntax.Operand
            var boundOperand = BindExpression(syntax.Operand);
            var boundOperator = BoundUnaryOperator.Bind(syntax.OperatorToken.Kind, boundOperand.Type);

            if (boundOperator == null)
            {
                _diagnostics.Add($"Unary operator '{syntax.OperatorToken.Text}' is not defined for type {boundOperand.Type}");
                return boundOperand;
            }

            return new BoundUnaryExpression(boundOperator, boundOperand);
        }

        private BoundUnaryOperatorKind? BindUnaryOperatorKind(SyntaxKind kind, Type operandType)
        {
            if (operandType == typeof(int))
            {
                switch (kind)
                {
                    case SyntaxKind.PlusToken:
                        return BoundUnaryOperatorKind.Identity;

                    case SyntaxKind.MinusToken:
                        return BoundUnaryOperatorKind.Negation;
                }
            }

            if (operandType == typeof(bool))
            {
                switch (kind)
                {
                    case SyntaxKind.BangToken:
                        return BoundUnaryOperatorKind.LogicalNegation;
                }
            }

            return null;
        }

        private BoundExpression BindBinaryExpression(BinaryExpressionSyntax syntax)
        {
            var left = BindExpression(syntax.Left);
            var right = BindExpression(syntax.Right);
            var boundOperator = BoundBinaryOperator.Bind(syntax.OperatorToken.Kind, left.Type, right.Type);

            if (boundOperator == null)
            {
                _diagnostics.Add($"Binary operator '{syntax.OperatorToken.Text}' is not defined for types {left.Type} and {right.Type}");
                return left;
            }
            return new BoundBinaryExpression(left, boundOperator, right);
        }

        private BoundBinaryOperatorKind? BindBinaryOperatorKind(SyntaxKind kind, Type leftType, Type rightType)
        {
            if (leftType == typeof(int) && rightType == typeof(int))
            {
                switch (kind)
                {
                    case SyntaxKind.PlusToken:
                        return BoundBinaryOperatorKind.Addition;

                    case SyntaxKind.MinusToken:
                        return BoundBinaryOperatorKind.Subtraction;

                    case SyntaxKind.SlashToken:
                        return BoundBinaryOperatorKind.Division;

                    case SyntaxKind.StarToken:
                        return BoundBinaryOperatorKind.Multiplication;
                }
            }

            if (leftType == typeof(bool) && rightType == typeof(bool))
            {
                switch (kind)
                {
                    case SyntaxKind.AmpersandAmpersandToken:
                        return BoundBinaryOperatorKind.LogicalAnd;

                    case SyntaxKind.PipePipeToken:
                        return BoundBinaryOperatorKind.LogicalOr;
                }
            }

            return null;
        }

        private BoundExpression BindLiteralExpression(LiteralExpressionSyntax syntax)
        {
            var value = syntax.Value ?? 0;

            return new BoundLiteralExpression(value);
        }
    }
}