using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Lowering
{
    sealed class Lowerer : BoundTreeRewriter
    {
        private Lowerer()
        {
        }

        public static BoundStatement Lower(BoundStatement statement)
        {
            var lowerer = new Lowerer();
            return lowerer.RewriteStatement(statement);
        }

        protected override BoundExpression RewriteForExpression(BoundForExpression node)
        {
            /*
             * convert from for to while
             *
             * for (x <- l to u) expr
             *
             * var x = l
             * while(x < u) {
             *     expr
             *     x = x + 1
             * }
             */
            var declareX = new BoundVariableDeclarationStatement(node.Variable, node.LowerBound);

            var condition = new BoundBinaryExpression(
                new BoundVariableExpression(node.Variable),
                BoundBinaryOperator.Bind(SyntaxKind.LessThanToken, typeof(int), typeof(int)),
                node.UpperBound
            );

            var incrementX = new BoundExpressionStatement(
                new BoundAssignmentExpression(
                    node.Variable,
                    new BoundBinaryExpression(
                        new BoundVariableExpression(node.Variable),
                        BoundBinaryOperator.Bind(SyntaxKind.PlusToken, typeof(int), typeof(int)),
                        new BoundLiteralExpression(1)
                    )
                ));
            var whileBody = new BoundBlockExpression(
                ImmutableArray.Create<BoundStatement>(
                    new BoundExpressionStatement(node.Body),
                    incrementX
                ), new BoundUnitExpression());

            var newBlock = new BoundBlockExpression(
                ImmutableArray.Create<BoundStatement>(declareX),
                new BoundWhileExpression(
                    condition,
                    whileBody
                )
            );
            return RewriteExpression(newBlock);
        }
    }
}