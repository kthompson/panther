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

    public static TypedStatement Lower(Symbol method, TypedStatement statement) =>
        Getters.Lower(Setters.Lower(statement));

    class Setters : TypedTreeRewriter
    {
        protected override TypedExpression RewriteAssignmentExpression(
            TypedAssignmentExpression node
        )
        {
            if (node.Left is TypedIndexExpression indexExpression)
            {
                return new TypedCallExpression(
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

        public static TypedStatement Lower(TypedStatement statement) =>
            new Setters().RewriteStatement(statement);
    }

    class Getters : TypedTreeRewriter
    {
        protected override TypedExpression RewriteIndexExpression(
            TypedIndexExpression indexExpression
        ) =>
            new TypedCallExpression(
                indexExpression.Syntax,
                indexExpression.Getter!,
                RewriteExpression(indexExpression.Expression),
                ImmutableArray.Create(RewriteExpression(indexExpression.Index))
            );

        public static TypedStatement Lower(TypedStatement statement) =>
            new Getters().RewriteStatement(statement);
    }
}
