using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Lowering
{
    sealed class Lowerer : BoundTreeRewriter
    {
        private int _labelCount;
        private int _variableCount;

        enum LabelToken
        {
        }

        private Lowerer()
        {
        }

        private LabelToken GenerateLabelToken()
        {
            _labelCount++;
            return (LabelToken) _labelCount;
        }

        private BoundLabel GenerateLabel(string tag)
        {
            var token = GenerateLabelToken();
            return GenerateLabel(tag, token);
        }

        private BoundLabel GenerateLabel(string tag, LabelToken token) =>
            new BoundLabel($"{tag}Label{(int) token}");

        private VariableSymbol GenerateVariable(TypeSymbol type)
        {
            _variableCount++;
            return new VariableSymbol($"variable${_variableCount}", false, type);
        }

        public static BoundBlockExpression Lower(BoundStatement statement)
        {
            var lowerer = new Lowerer();
            var boundStatement = lowerer.RewriteStatement(statement);
            return FlattenBlocks(boundStatement);
        }

        /// <summary>
        /// Takes all block expressions and flattens them into a single block expression/statement
        /// </summary>
        /// <param name="boundStatement"></param>
        /// <returns></returns>
        private static BoundBlockExpression FlattenBlocks(BoundStatement boundStatement)
        {
            var statements = new List<BoundStatement>();
            var work = new Stack<BoundStatement>();
            work.Push(boundStatement);

            while (work.Count > 0)
            {
                var current = work.Pop();
                if (current is BoundExpressionStatement expressionStatement &&
                    expressionStatement.Expression is BoundBlockExpression block)
                {
                    work.Push(new BoundExpressionStatement(block.Expression));
                    foreach (var statement in block.Statements.Reverse())
                    {
                        work.Push(statement);
                    }
                }
                else if (current is BoundVariableDeclarationStatement variableDeclarationStatement &&
                         variableDeclarationStatement.Expression is BoundBlockExpression variableDeclBlock)
                {
                    work.Push(new BoundExpressionStatement(
                        new BoundVariableExpression(variableDeclarationStatement.Variable)));
                    work.Push(new BoundVariableDeclarationStatement(variableDeclarationStatement.Variable,
                        variableDeclBlock.Expression));

                    foreach (var statement in variableDeclBlock.Statements.Reverse())
                        work.Push(statement);
                }
                else
                {
                    statements.Add(current);
                }
            }

            var expr = (statements.LastOrDefault() as BoundExpressionStatement)?.Expression;
            var stmts = expr == null ? statements : statements.Take(statements.Count - 1);

            expr ??= BoundUnitExpression.Default;

            return new BoundBlockExpression(stmts.ToImmutableArray(), expr);
        }

        protected override BoundExpression RewriteWhileExpression(BoundWhileExpression node)
        {
            /*
             * while <condition>
             *     <body>
             *
             * <whileLabel>
             * gotoIfFalse <condition> <endLabel>
             *     <body>
             *     goto <whileLabel>
             * <endLabel>
             */

            var token = GenerateLabelToken();
            var whileLabel = GenerateLabel("While", token);
            var endLabel = GenerateLabel("EndWhile", token);
            var body = RewriteExpression(node.Body);
            var condition = RewriteExpression(node.Condition);
            return RewriteExpression(
                new BoundBlockExpression(
                    ImmutableArray.Create<BoundStatement>(
                        new BoundLabelStatement(whileLabel),
                        new BoundConditionalGotoStatement(endLabel, condition),
                        new BoundExpressionStatement(body),
                        new BoundGotoStatement(whileLabel),
                        new BoundLabelStatement(endLabel)
                    ),
                    BoundUnitExpression.Default
                )
            );
        }

        protected override BoundExpression RewriteIfExpression(BoundIfExpression node)
        {
            /*
             * if (<conditon>)
             *     <thenBody>
             * else
             *     <elseBody>
             *
             *
             * to
             *
             * gotoIfFalse <conditon> <elseLabel>
             *    <thenBody>
             *    goto <endLabel>
             * <elseLabel>
             *     <elseBody>
             * <endLabel>
             *
             */

            var token = GenerateLabelToken();
            var elseLabel = GenerateLabel("IfElse", token);
            var endLabel = GenerateLabel("IfEnd", token);
            var variable = GenerateVariable(node.Type);

            var condition = RewriteExpression(node.Condition);
            var then = RewriteExpression(node.Then);
            var @else = RewriteExpression(node.Else);
            var block = new BoundBlockExpression(
                ImmutableArray.Create<BoundStatement>(
                    new BoundConditionalGotoStatement(elseLabel, condition),
                    new BoundVariableDeclarationStatement(variable, then),
                    new BoundGotoStatement(endLabel),
                    new BoundLabelStatement(elseLabel),
                    new BoundVariableDeclarationStatement(variable, @else),
                    new BoundLabelStatement(endLabel)
                ),
                new BoundVariableExpression(variable)
            );

            return RewriteExpression(block);
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

            var lowerBound = RewriteExpression(node.LowerBound);
            var upperBound = RewriteExpression(node.UpperBound);
            var body = RewriteExpression(node.Body);

            var declareX = new BoundVariableDeclarationStatement(node.Variable, lowerBound);

            var condition = new BoundBinaryExpression(
                new BoundVariableExpression(node.Variable),
                BoundBinaryOperator.Bind(SyntaxKind.LessThanToken, TypeSymbol.Int, TypeSymbol.Int),
                upperBound
            );

            var incrementX = new BoundExpressionStatement(
                new BoundAssignmentExpression(
                    node.Variable,
                    new BoundBinaryExpression(
                        new BoundVariableExpression(node.Variable),
                        BoundBinaryOperator.Bind(SyntaxKind.PlusToken, TypeSymbol.Int, TypeSymbol.Int),
                        new BoundLiteralExpression(1)
                    )
                ));
            var whileBody = new BoundBlockExpression(
                ImmutableArray.Create<BoundStatement>(
                    new BoundExpressionStatement(body),
                    incrementX
                ), BoundUnitExpression.Default
            );

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