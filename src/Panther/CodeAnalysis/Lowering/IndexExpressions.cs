using System;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Lowering;

sealed class IndexExpressions
{
    private readonly Symbol _method;

    private IndexExpressions(Symbol method)
    {
        _method = method;
    }

    public static BoundStatement Lower(Symbol method, BoundStatement statement) =>
        Getters.Lower(Setters.Lower(statement));

    class Setters : BoundTreeRewriter
    {
        protected override BoundExpression RewriteAssignmentExpression(
            BoundAssignmentExpression node
        )
        {
            if (node.Left is BoundIndexExpression indexExpression)
            {
                return new BoundCallExpression(
                    node.Syntax,
                    indexExpression.Setter!,
                    RewriteExpression(indexExpression.Expression),
                    ImmutableArray.Create(
                        RewriteExpression(indexExpression.Index),
                        RewriteExpression(node.Right)
                    )
                );
            }

            return node;
        }

        public static BoundStatement Lower(BoundStatement statement) =>
            new Setters().RewriteStatement(statement);
    }

    class Getters : BoundTreeRewriter
    {
        protected override BoundExpression RewriteIndexExpression(
            BoundIndexExpression indexExpression
        ) =>
            new BoundCallExpression(
                indexExpression.Syntax,
                indexExpression.Getter!,
                RewriteExpression(indexExpression.Expression),
                ImmutableArray.Create(RewriteExpression(indexExpression.Index))
            );

        public static BoundStatement Lower(BoundStatement statement) =>
            new Getters().RewriteStatement(statement);
    }
}
