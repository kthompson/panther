using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Binding;

namespace Panther.CodeAnalysis.Lowering
{
    internal class BoundStatementListRewriter : BoundTreeRewriter
    {
        protected readonly List<BoundStatement> _statements = new List<BoundStatement>();

        protected BoundStatementListRewriter()
        {
        }

        protected BoundBlockExpression GetBlock()
        {
            var expr = (_statements.LastOrDefault() as BoundExpressionStatement)?.Expression;
            var stmts = expr == null ? _statements : _statements.Take(_statements.Count - 1);

            expr ??= BoundUnitExpression.Default;

            return new BoundBlockExpression(stmts.ToImmutableArray(), expr);
        }


        protected BoundBlockExpression Rewrite(BoundBlockExpression block)
        {
            foreach (var statement in block.Statements) RewriteStatement(statement);

            RewriteStatement(new BoundExpressionStatement(block.Expression));

            return GetBlock();
        }

        protected override BoundStatement RewriteStatement(BoundStatement node)
        {
            var statement = base.RewriteStatement(node);
            if (statement != null)
                _statements.Add(statement);

            return statement;
        }
    }
}