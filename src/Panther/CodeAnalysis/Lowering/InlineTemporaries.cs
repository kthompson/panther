using System;
using System.Collections.Generic;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Lowering
{
    sealed class InlineTemporaries : BoundStatementListRewriter
    {
        private readonly Dictionary<VariableSymbol, BoundExpression> _expressionsToInline = new Dictionary<VariableSymbol, BoundExpression>();
        private InlineTemporaries()
        {
        }


        protected override BoundStatement RewriteStatement(BoundStatement node)
        {
            if (node is BoundVariableDeclarationStatement varDecl && varDecl.Variable.Name.StartsWith("temp$"))
            {
                _expressionsToInline[varDecl.Variable] = varDecl.Expression;
                return null;
            }

            return base.RewriteStatement(node);
        }

        protected override BoundExpression RewriteExpression(BoundExpression node)
        {
            if (node is BoundVariableExpression variableExpression && _expressionsToInline.TryGetValue(variableExpression.Variable, out var expression))
            {
                _expressionsToInline.Remove(variableExpression.Variable);
                return RewriteExpression(expression);
            }

            return base.RewriteExpression(node);
        }

        protected override BoundExpression RewriteBlockExpression(BoundBlockExpression node)
        {
            if (node.Statements.IsEmpty)
            {

            }
            return base.RewriteBlockExpression(node);
        }

        public static BoundBlockExpression Lower(BoundBlockExpression blockExpression) =>
            new InlineTemporaries().Rewrite(blockExpression);
    }
}