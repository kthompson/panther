using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Lowering
{
    internal sealed class Lowerer : BoundTreeRewriter
    {
        private int _labelCount;
        private int _variableCount;

        private enum LabelToken
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

        private VariableSymbol GenerateVariable(Type type)
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

            return new BoundBlockExpression(block.Syntax, builder.ToImmutable(), block.Expression);
        }

        protected override BoundStatement RewriteBoundConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            var constant = node.Condition.ConstantValue;
            if (constant == null)
                return base.RewriteBoundConditionalGotoStatement(node);

            var condition = (bool)constant.Value;

            var condition2 = node.JumpIfTrue ? condition : !condition;
            if (condition2)
                return new BoundGotoStatement(node.Syntax, node.BoundLabel);

            return new BoundNopStatement(node.Syntax);
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
                new BoundBlockExpression(node.Syntax,
                    ImmutableArray.Create<BoundStatement>(
                        new BoundLabelStatement(node.Syntax, node.ContinueLabel),
                        new BoundConditionalGotoStatement(node.Syntax, node.BreakLabel, condition),
                        new BoundExpressionStatement(node.Syntax, body),
                        new BoundGotoStatement(node.Syntax, node.ContinueLabel),
                        new BoundLabelStatement(node.Syntax, node.BreakLabel)
                    ),
                    new BoundUnitExpression(node.Syntax)
                )
            );
        }

        protected override BoundExpression RewriteIfExpression(BoundIfExpression node)
        {
            /*
             * if (<condition>)
             *     <thenBody>
             * else
             *     <elseBody>
             *
             *
             * to
             *
             * gotoIfFalse <condition> <elseLabel>
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
            var block = new BoundBlockExpression(node.Syntax,
                ImmutableArray.Create<BoundStatement>(
                    new BoundConditionalGotoStatement(node.Syntax, elseLabel, condition),
                    new BoundVariableDeclarationStatement(node.Syntax, variable, then),
                    new BoundGotoStatement(node.Syntax, endLabel),
                    new BoundLabelStatement(node.Syntax, elseLabel),
                    new BoundVariableDeclarationStatement(node.Syntax, variable, @else),
                    new BoundLabelStatement(node.Syntax, endLabel)
                ),
                new BoundVariableExpression(node.Syntax, variable)
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

            var declareX = new BoundVariableDeclarationStatement(node.Syntax, node.Variable, lowerBound);

            var condition = new BoundBinaryExpression(node.Syntax,
                new BoundVariableExpression(node.Syntax, node.Variable),
                BoundBinaryOperator.BindOrThrow(SyntaxKind.LessThanToken, Type.Int, Type.Int),
                upperBound
            );
            var continueLabelStatement = new BoundLabelStatement(node.Syntax, node.ContinueLabel);
            var incrementX = new BoundExpressionStatement(
                node.Syntax,
                new BoundAssignmentExpression(node.Syntax, node.Variable, new BoundBinaryExpression(
                    node.Syntax,
                    new BoundVariableExpression(node.Syntax, node.Variable),
                    BoundBinaryOperator.BindOrThrow(SyntaxKind.PlusToken, Type.Int, Type.Int),
                    new BoundLiteralExpression(node.Syntax, 1)
                )));
            var whileBody = new BoundBlockExpression(
                node.Syntax,
                ImmutableArray.Create<BoundStatement>(
                    new BoundExpressionStatement(body.Syntax, body),
                    continueLabelStatement,
                    incrementX
                ), new BoundUnitExpression(node.Syntax)
            );

            var newBlock = new BoundBlockExpression(
                node.Syntax,
                ImmutableArray.Create<BoundStatement>(declareX),
                new BoundWhileExpression(node.Syntax, condition, whileBody, node.BreakLabel, new BoundLabel("continue"))
            );
            return RewriteExpression(newBlock);
        }
    }
}