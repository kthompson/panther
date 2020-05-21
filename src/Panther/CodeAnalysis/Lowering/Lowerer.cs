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
            return (LabelToken)_labelCount;
        }

        private BoundLabel GenerateLabel(string tag)
        {
            var token = GenerateLabelToken();
            return GenerateLabel(tag, token);
        }

        private BoundLabel GenerateLabel(string tag, LabelToken token) =>
            new BoundLabel($"{tag}Label{(int)token}");

        private VariableSymbol GenerateVariable(TypeSymbol type)
        {
            _variableCount++;
            return new LocalVariableSymbol($"variable${_variableCount}", false, type, null);
        }

        public static BoundBlockExpression Lower(BoundStatement statement)
        {
            var debug = false;
            if (debug)
            {
                Console.WriteLine("==== Original Code ===");
                statement.WriteTo(Console.Out);
            }

            var lowerer = new Lowerer();
            var boundStatement = lowerer.RewriteStatement(statement);
            if (debug)
            {
                Console.WriteLine("==== Lowered Code ===");
                boundStatement.WriteTo(Console.Out);
            }

            var tac = ThreeAddressCode.Lower(boundStatement);
            if (debug)
            {
                Console.WriteLine("==== Three Address Code ===");
                tac.WriteTo(Console.Out);
            }


            var unitLessStatements = RemoveUnitAssignments.Lower(tac);
            if (debug)
            {
                Console.WriteLine("==== Remove Unit Assignments ===");
                unitLessStatements.WriteTo(Console.Out);
            }

            var inlinedTemporaries = InlineTemporaries.Lower(unitLessStatements);
            if (debug)
            {
                Console.WriteLine("==== Inlined Temporaries ===");
                inlinedTemporaries.WriteTo(Console.Out);
            }

            var deadCodeRemoval = RemoveDeadCode(inlinedTemporaries);
            if (debug)
            {
                Console.WriteLine("==== Dead Code Removal ===");
                deadCodeRemoval.WriteTo(Console.Out);
            }

            return deadCodeRemoval;
        }


        private static BoundBlockExpression RemoveDeadCode(BoundBlockExpression block)
        {
            var controlFlow = ControlFlowGraph.Create(block);
            var reachableStatements = new HashSet<BoundStatement>(controlFlow.Blocks.SelectMany(basicBlock => basicBlock.Statements));

            var builder = block.Statements.ToBuilder();
            for (int i = builder.Count - 1; i >= 0; i--)
            {
                if (!reachableStatements.Contains(builder[i]))
                    builder.RemoveAt(i);
            }

            return new BoundBlockExpression(builder.ToImmutable(), block.Expression);
        }

        protected override BoundStatement RewriteBoundConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            var constant = node.Condition.ConstantValue;
            if (constant == null)
                return base.RewriteBoundConditionalGotoStatement(node);

            var condition = (bool)constant.Value;

            var condition2 = node.JumpIfTrue ? condition : !condition;
            if (condition2)
                return new BoundGotoStatement(node.BoundLabel);

            return BoundNopStatement.Default;
        }


        protected override BoundExpression RewriteBlockExpression(BoundBlockExpression node)
        {
            if (node.Statements.Length == 0)
                return RewriteExpression(node.Expression);

            return base.RewriteBlockExpression(node);
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

            var body = RewriteExpression(node.Body);
            var condition = RewriteExpression(node.Condition);
            return RewriteExpression(
                new BoundBlockExpression(
                    ImmutableArray.Create<BoundStatement>(
                        new BoundLabelStatement(node.ContinueLabel),
                        new BoundConditionalGotoStatement(node.BreakLabel, condition),
                        new BoundExpressionStatement(body),
                        new BoundGotoStatement(node.ContinueLabel),
                        new BoundLabelStatement(node.BreakLabel)
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
             *     continue:
             *     x = x + 1
             * }
             */

            var lowerBound = RewriteExpression(node.LowerBound);
            var upperBound = RewriteExpression(node.UpperBound);
            var body = RewriteExpression(node.Body);

            var declareX = new BoundVariableDeclarationStatement(node.Variable, lowerBound);

            var condition = new BoundBinaryExpression(
                new BoundVariableExpression(node.Variable),
                BoundBinaryOperator.BindOrThrow(SyntaxKind.LessThanToken, TypeSymbol.Int, TypeSymbol.Int),
                upperBound
            );
            var continueLabelStatement = new BoundLabelStatement(node.ContinueLabel);
            var incrementX = new BoundExpressionStatement(
                new BoundAssignmentExpression(
                    node.Variable,
                    new BoundBinaryExpression(
                        new BoundVariableExpression(node.Variable),
                        BoundBinaryOperator.BindOrThrow(SyntaxKind.PlusToken, TypeSymbol.Int, TypeSymbol.Int),
                        new BoundLiteralExpression(1)
                    )
                ));
            var whileBody = new BoundBlockExpression(
                ImmutableArray.Create<BoundStatement>(
                    new BoundExpressionStatement(body),
                    continueLabelStatement,
                    incrementX
                ), BoundUnitExpression.Default
            );

            var newBlock = new BoundBlockExpression(
                ImmutableArray.Create<BoundStatement>(declareX),
                new BoundWhileExpression(
                    condition,
                    whileBody,
                    node.BreakLabel,
                    new BoundLabel("continue")
                )
            );
            return RewriteExpression(newBlock);
        }
    }
}