using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Lowering
{
    sealed class BlockFlattener : BoundTreeRewriter
    {
        private readonly List<BoundStatement> _statements = new List<BoundStatement>();
        private int _tempCount = 0;

        private BlockFlattener()
        {
        }

        public BoundBlockExpression GetBlock()
        {
            var expr = (_statements.LastOrDefault() as BoundExpressionStatement)?.Expression;
            var stmts = expr == null ? _statements : _statements.Take(_statements.Count - 1);

            expr ??= BoundUnitExpression.Default;

            return new BoundBlockExpression(stmts.ToImmutableArray(), expr);
        }

        public static BoundBlockExpression FlattenBlocks(BoundStatement boundStatement)
        {
            var flattener = new BlockFlattener();
            flattener.RewriteStatement(boundStatement);
            return flattener.GetBlock();
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
            var tempVariable = CreateTemporary(rewritten);

            return new BoundVariableExpression(tempVariable);
        }

        protected override BoundStatement RewriteStatement(BoundStatement node)
        {
            var statement = base.RewriteStatement(node);
            _statements.Add(statement);
            return statement;
        }

        protected override BoundExpression RewriteCallExpression(BoundCallExpression node)
        {
            // since statements can exist inside of a block which is an argument, when we flatten the block statements
            // can get out of order if there are side effects in any of the arguments. In order to prevent this we need
            // to break out the evaluation of each argument and assign to a temporary variable in the correct order.
            // we can then access this temp variable later when we call the function
            var args = node.Arguments
                .Select(RewriteExpression)
                .Select(CreateTemporary)
                .Select(argTemp => new BoundVariableExpression(argTemp))
                .Cast<BoundExpression>()
                .ToImmutableArray();

            return new BoundCallExpression(node.Function, args);
        }

        private LocalVariableSymbol CreateTemporary(BoundExpression boundExpression)
        {
            _tempCount++;
            var name = $"temp{_tempCount:0000}";
            var tempVariable = new LocalVariableSymbol(name, true, boundExpression.Type);
            _statements.Add(new BoundVariableDeclarationStatement(tempVariable, boundExpression));
            return tempVariable;
        }
    }
}