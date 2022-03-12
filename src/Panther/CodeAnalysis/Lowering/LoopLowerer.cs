using System.Collections.Immutable;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Lowering
{
    internal sealed class LoopLowerer : BoundTreeRewriter
    {
        private readonly Symbol _method;
        private int _labelCount;
        private int _variableCount;

        private enum LabelToken
        {
        }

        private LoopLowerer(Symbol method)
        {
            _method = method;
        }


        public static BoundStatement Lower(Symbol method, BoundStatement statement) =>
            new LoopLowerer(method).RewriteStatement(statement);

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

        private Symbol GenerateVariable(Type type)
        {
            _variableCount++;
            var name = $"variable${_variableCount}";

            return _method
                .NewLocal(TextLocation.None, name, false)
                .WithType(type)
                .Declare();
        }

        protected override BoundStatement RewriteBoundConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            var constant = ConstantFolding.Fold(node.Condition);
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
            if (condition is BoundLiteralExpression literal)
                return RewriteExpression((bool)literal.Value ? node.Then : node.Else);

            var then = RewriteExpression(node.Then);
            var @else = RewriteExpression(node.Else);
            var variableExpression = new BoundVariableExpression(node.Syntax, variable);
            var block = new BoundBlockExpression(node.Syntax,
                ImmutableArray.Create<BoundStatement>(
                    new BoundVariableDeclarationStatement(node.Syntax, variable, null),
                    new BoundConditionalGotoStatement(node.Syntax, elseLabel, condition),
                    new BoundAssignmentStatement(node.Syntax, variableExpression, then),
                    new BoundGotoStatement(node.Syntax, endLabel),
                    new BoundLabelStatement(node.Syntax, elseLabel),
                    new BoundAssignmentStatement(node.Syntax, variableExpression, @else),
                    new BoundLabelStatement(node.Syntax, endLabel)
                ),
                variableExpression
            );

            return RewriteExpression(block);
        }

        private BoundExpression ValueExpression(SyntaxNode syntax, Symbol symbol)
        {
            if (symbol.IsField)
                return new BoundFieldExpression(syntax, null, symbol);

            return new BoundVariableExpression(syntax, symbol);
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

            var variableExpression = ValueExpression(node.Syntax, node.Variable);
            var condition = new BoundBinaryExpression(node.Syntax,
                variableExpression,
                BoundBinaryOperator.BindOrThrow(SyntaxKind.LessThanToken, Type.Int, Type.Int),
                upperBound
            );
            var continueLabelStatement = new BoundLabelStatement(node.Syntax, node.ContinueLabel);
            var incrementX = new BoundExpressionStatement(
                node.Syntax,
                new BoundAssignmentExpression(node.Syntax, variableExpression,
                    new BoundBinaryExpression(
                        node.Syntax,
                        variableExpression,
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