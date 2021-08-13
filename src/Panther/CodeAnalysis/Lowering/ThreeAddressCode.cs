using System;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Lowering
{
    sealed class ThreeAddressCode : BoundStatementListRewriter
    {
        private int _tempCount = 0;

        private ThreeAddressCode()
        {
        }

        public static BoundBlockExpression Lower(BoundStatement boundStatement)
        {
            var tac = new ThreeAddressCode();
            tac.RewriteStatement(boundStatement);
            return tac.GetBlock(boundStatement.Syntax);
        }


        protected override BoundExpression RewriteBlockExpression(BoundBlockExpression node)
        {
            if (node.Statements.Length == 0)
                return RewriteExpression(node.Expression);

            foreach (var boundStatement in node.Statements)
            {
                // has side effect and will be added to the list of statements
                RewriteStatement(boundStatement);
            }

            var rewritten = this.RewriteExpression(node.Expression);
            if (rewritten.Kind == BoundNodeKind.LiteralExpression)
                return rewritten;

            // node.Expression had a nested block so assign it to a temp and return the variable
            return CreateTemporary(rewritten);
        }

        protected override BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            // since statements can exist inside of a block which is an argument, when we flatten the block statements
            // can get out of order if there are side effects in any of the arguments. In order to prevent this we need
            // to break out the evaluation of each argument and assign to a temporary variable in the correct order.
            // we can then access this temp variable later when we call the function
            var args = node.Arguments
                .Select(RewriteExpression)
                .Select(expr => CreateTemporary(expr, "ctemp"))
                .ToImmutableArray();

            return new BoundCallExpression(node.Syntax, node.Method, node.Expression, args);
        }

        protected override BoundExpression RewriteBinaryExpression(BoundBinaryExpression node)
        {
            var left = IsSimpleNode(node.Left) ? node.Left : CreateTemporary(node.Left);
            var right = IsSimpleNode(node.Right) ? node.Right : CreateTemporary(node.Right);
            var @operator = RewriteBinaryOperator(node.Operator);

            if (node.Left == left && node.Right == right && node.Operator == @operator)
                return node;

            return new BoundBinaryExpression(node.Syntax, left, @operator, right);
        }

        protected override BoundExpression RewriteUnaryExpression(BoundUnaryExpression node)
        {
            var rewrittenOp = RewriteExpression(node.Operand);
            var operand = IsSimpleNode(rewrittenOp) ? rewrittenOp : CreateTemporary(rewrittenOp);
            var @operator = RewriteUnaryOperator(node.Operator);
            if (node.Operand == operand && node.Operator == @operator)
                return node;

            return new BoundUnaryExpression(node.Syntax, @operator, operand);
        }

        protected override BoundExpression RewriteAssignmentExpression(BoundAssignmentExpression node)
        {
            RewriteStatement(new BoundAssignmentStatement(node.Syntax, node.Variable, node.Expression));

            return new BoundUnitExpression(node.Syntax);
        }

        protected override BoundExpression RewriteForExpression(BoundForExpression node)
        {
            throw new InvalidProgramException("No `for` expression should exist at this stage");
        }

        protected override BoundExpression RewriteWhileExpression(BoundWhileExpression node)
        {
            throw new InvalidProgramException("No `while` expression should exist at this stage");
        }

        protected override BoundExpression RewriteConversionExpression(BoundConversionExpression node)
        {
            var rewriteExpression = RewriteExpression(node.Expression);
            var expr = IsSimpleNode(rewriteExpression) ? rewriteExpression : CreateTemporary(rewriteExpression);
            if (expr == node.Expression)
                return node;

            return new BoundConversionExpression(node.Syntax, node.Type, expr);
        }

        private static bool IsSimpleNode(BoundExpression node) =>
            node.Kind == BoundNodeKind.VariableExpression || node.Kind == BoundNodeKind.LiteralExpression;

        private BoundExpression CreateTemporary(BoundExpression boundExpression, string prefix = "temp")
        {
            _tempCount++;
            var name = $"{prefix}${_tempCount:0000}";
            var tempVariable = new LocalVariableSymbol(name, true, boundExpression.Type, boundExpression.ConstantValue);
            _statements.Add(new BoundVariableDeclarationStatement(boundExpression.Syntax, tempVariable, boundExpression));
            return new BoundVariableExpression(boundExpression.Syntax, tempVariable);
        }
    }
}