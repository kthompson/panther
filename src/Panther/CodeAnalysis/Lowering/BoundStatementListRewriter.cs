using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Lowering
{
    internal class BoundStatementListRewriter : BoundTreeRewriter
    {
        protected readonly List<BoundStatement> _statements = new List<BoundStatement>();

        protected BoundStatementListRewriter()
        {
        }

        protected BoundBlockExpression GetBlock(SyntaxNode syntax)
        {
            var expr = (_statements.LastOrDefault() as BoundExpressionStatement)?.Expression;
            var stmts = expr == null ? _statements : _statements.Take(_statements.Count - 1);

            expr ??= new BoundUnitExpression(syntax);

            return new BoundBlockExpression(syntax, stmts.ToImmutableArray(), expr);
        }


        protected BoundBlockExpression Rewrite(BoundBlockExpression block)
        {
            foreach (var statement in block.Statements)
                RewriteStatement(statement);

            RewriteStatement(new BoundExpressionStatement(block.Syntax, block.Expression));

            return GetBlock(block.Syntax);
        }

        protected override BoundStatement RewriteStatement(BoundStatement node)
        {
            var statement = base.RewriteStatement(node);

            if (statement is not BoundNopStatement)
                _statements.Add(statement);

            return statement;
        }
    }
}