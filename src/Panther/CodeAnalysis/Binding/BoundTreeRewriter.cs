using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Panther.CodeAnalysis.Binding
{
    internal abstract class BoundTreeRewriter
    {
        protected virtual BoundExpression RewriteExpression(BoundExpression node) =>
            node switch
            {
                BoundCallExpression callExpression => RewriteCallExpression(callExpression),
                BoundConversionExpression conversionExpression => RewriteConversionExpression(conversionExpression),
                BoundErrorExpression errorExpression => RewriteErrorExpression(errorExpression),
                BoundAssignmentExpression boundAssignmentExpression => RewriteAssignmentExpression(boundAssignmentExpression),
                BoundBinaryExpression boundBinaryExpression => RewriteBinaryExpression(boundBinaryExpression),
                BoundBlockExpression boundBlockExpression => RewriteBlockExpression(boundBlockExpression),
                BoundForExpression boundForExpression => RewriteForExpression(boundForExpression),
                BoundIfExpression boundIfExpression => RewriteIfExpression(boundIfExpression),
                BoundLiteralExpression boundLiteralExpression => RewriteLiteralExpression(boundLiteralExpression),
                BoundUnaryExpression boundUnaryExpression => RewriteUnaryExpression(boundUnaryExpression),
                BoundUnitExpression boundUnitExpression => RewriteUnitExpression(boundUnitExpression),
                BoundVariableExpression boundVariableExpression => RewriteVariableExpression(boundVariableExpression),
                BoundWhileExpression boundWhileExpression => RewriteWhileExpression(boundWhileExpression),
                _ => throw new ArgumentOutOfRangeException(nameof(node))
            };

        protected virtual BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            var expr = RewriteExpression(node.Expression);
            if (expr == node.Expression)
                return node;

            return new BoundConversionExpression(node.Type, expr);
        }

        protected virtual BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            List<BoundExpression>? newArguments = null;

            for (var i = 0; i < node.Arguments.Length; i++)
            {
                var argument = node.Arguments[i];
                var newArgument = RewriteExpression(argument);

                if (newArgument != argument)
                {
                    if (newArguments == null)
                    {
                        // initialize the list with all the statements up to `i`
                        newArguments = new List<BoundExpression>();

                        for (var j = 0; j < i; j++)
                        {
                            newArguments.Add(node.Arguments[j]);
                        }
                    }
                }

                newArguments?.Add(newArgument);
            }

            if (newArguments == null)
                return node;

            return new BoundCallExpression(node.Function, newArguments.ToImmutableArray());
        }

        protected virtual BoundExpression RewriteWhileExpression(BoundWhileExpression node)
        {
            var cond = RewriteExpression(node.Condition);
            var body = RewriteExpression(node.Body);

            if (node.Condition == cond && node.Body == body)
                return node;

            return new BoundWhileExpression(cond, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            var operand = RewriteExpression(node.Operand);
            var @operator = RewriteUnaryOperator(node.Operator);
            if (node.Operand == operand && node.Operator == @operator)
                return node;

            return new BoundUnaryExpression(@operator, operand);
        }

        protected virtual BoundExpression RewriteIfExpression(BoundIfExpression node)
        {
            var cond = RewriteExpression(node.Condition);
            var then = RewriteExpression(node.Then);
            var @else = RewriteExpression(node.Else);

            if (node.Condition == cond && node.Then == then && node.Else == @else)
                return node;

            return new BoundIfExpression(cond, then, @else);
        }

        protected virtual BoundExpression RewriteForExpression(BoundForExpression node)
        {
            var lowerBound = RewriteExpression(node.LowerBound);
            var upperBound = RewriteExpression(node.UpperBound);
            var body = RewriteExpression(node.Body);

            if (node.LowerBound == lowerBound && node.UpperBound == upperBound && node.Body == body)
                return node;

            return new BoundForExpression(node.Variable, lowerBound, upperBound, body, node.BreakLabel, node.ContinueLabel);
        }

        protected virtual BoundExpression RewriteBlockExpression(BoundBlockExpression node)
        {
            List<BoundStatement>? statements = null;

            for (var i = 0; i < node.Statements.Length; i++)
            {
                var oldStatement = node.Statements[i];
                var newStatement = RewriteStatement(oldStatement);

                if (newStatement != oldStatement)
                {
                    if (statements == null)
                    {
                        // initialize the list with all the statements up to `i`
                        statements = new List<BoundStatement>();

                        for (var j = 0; j < i; j++)
                        {
                            statements.Add(node.Statements[j]);
                        }
                    }
                }

                if (newStatement != null)
                    statements?.Add(newStatement);
            }

            var expression = RewriteExpression(node.Expression);

            if (statements == null && node.Expression == expression)
                return node;

            if (statements == null)
                statements = node.Statements.ToList();

            return new BoundBlockExpression(statements.ToImmutableArray(), expression);
        }

        protected virtual BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            var left = RewriteExpression(node.Left);
            var right = RewriteExpression(node.Right);
            var @operator = RewriteBinaryOperator(node.Operator);

            if (node.Left == left && node.Right == right && node.Operator == @operator)
                return node;

            return new BoundBinaryExpression(left, @operator, right);
        }

        protected virtual BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            var expr = RewriteExpression(node.Expression);
            if (expr == node.Expression)
                return node;

            return new BoundAssignmentExpression(node.Variable, expr);
        }

        protected virtual BoundStatement RewriteStatement(BoundStatement node) =>
            node switch
            {
                BoundAssignmentStatement assignmentStatement => RewriteAssignmentStatement(assignmentStatement),
                BoundExpressionStatement expressionStatement => RewriteExpressionStatement(expressionStatement),
                BoundNopStatement nopStatement => RewriteNopStatement(nopStatement),
                BoundVariableDeclarationStatement variableDeclarationStatement => RewriteVariableDeclarationStatement(variableDeclarationStatement),
                BoundLabelStatement labelStatement => RewriteBoundLabelStatement(labelStatement),
                BoundGotoStatement gotoStatement => RewriteBoundGotoStatement(gotoStatement),
                BoundConditionalGotoStatement conditionalGotoStatement => RewriteBoundConditionalGotoStatement(conditionalGotoStatement),
                _ => throw new ArgumentOutOfRangeException(nameof(node), $"Unexpected kind: {node.Kind}")
            };

        protected virtual BoundStatement RewriteAssignmentStatement(BoundAssignmentStatement node)
        {
            var expr = RewriteExpression(node.Expression);
            if (expr == node.Expression)
                return node;

            return new BoundAssignmentStatement(node.Variable, expr);
        }

        protected virtual BoundStatement RewriteBoundConditionalGotoStatement(BoundConditionalGotoStatement node)
        {
            var cond = RewriteExpression(node.Condition);
            if (node.Condition == cond)
                return node;

            return new BoundConditionalGotoStatement(node.BoundLabel, cond, node.JumpIfTrue);
        }

        protected virtual BoundStatement RewriteExpressionStatement(BoundExpressionStatement node)
        {
            var expr = RewriteExpression(node.Expression);
            if (expr == node.Expression)
                return node;

            return new BoundExpressionStatement(expr);
        }

        protected virtual BoundStatement RewriteVariableDeclarationStatement(BoundVariableDeclarationStatement node)
        {
            var expr = RewriteExpression(node.Expression);
            if (expr == node.Expression)
                return node;

            return new BoundVariableDeclarationStatement(node.Variable, expr);
        }

        protected virtual BoundExpression RewriteErrorExpression(BoundErrorExpression node) => node;

        protected virtual BoundStatement RewriteNopStatement(BoundNopStatement node) => node;

        protected virtual BoundStatement RewriteBoundGotoStatement(BoundGotoStatement node) => node;

        protected virtual BoundStatement RewriteBoundLabelStatement(BoundLabelStatement node) => node;

        protected virtual BoundUnaryOperator RewriteUnaryOperator(BoundUnaryOperator node) => node;

        protected virtual BoundExpression RewriteVariableExpression(BoundVariableExpression node) => node;

        protected virtual BoundExpression RewriteUnitExpression(BoundUnitExpression node) => node;

        protected virtual BoundExpression RewriteLiteralExpression(BoundLiteralExpression node) => node;

        protected virtual BoundBinaryOperator RewriteBinaryOperator(BoundBinaryOperator node) => node;
    }
}