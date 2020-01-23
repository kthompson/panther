using System;
using System.Text;

namespace Panther.CodeAnalysis
{
    internal class Evaluator
    {
        private readonly ExpressionSyntax _root;

        public Evaluator(ExpressionSyntax root)
        {
            _root = root;
        }

        public int Evaluate()
        {
            return EvaluateExpression(_root);
        }

        private int EvaluateExpression(ExpressionSyntax node)
        {
            if (node is LiteralExpressionSyntax n)
            {
                return (int)n.LiteralToken.Value;
            }

            if (node is BinaryExpressionSyntax binaryExpression)
            {
                var left = EvaluateExpression(binaryExpression.Left);
                var right = EvaluateExpression(binaryExpression.Right);

                if (binaryExpression.OperationToken.Kind == SyntaxKind.PlusToken)
                    return left + right;

                if (binaryExpression.OperationToken.Kind == SyntaxKind.MinusToken)
                    return left - right;

                if (binaryExpression.OperationToken.Kind == SyntaxKind.StarToken)
                    return left * right;

                if (binaryExpression.OperationToken.Kind == SyntaxKind.SlashToken)
                    return left / right;

                throw new Exception($"Unexpected binary operator {binaryExpression.OperationToken.Text}");
            }

            if (node is GroupExpressionSyntax group)
            {
                return EvaluateExpression(group.Expression);
            }

            throw new Exception($"Unexpected expression {node.Kind}");
        }
    }
}