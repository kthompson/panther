using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Lowering;

sealed class RemoveUnitAssignments : TypedStatementListRewriter
{
    private RemoveUnitAssignments() { }

    protected override TypedStatement RewriteStatement(TypedStatement node)
    {
        TypedStatement RewriteSubExpression(TypedExpression original)
        {
            var expression = RewriteExpression(original);
            if (expression.Kind == TypedNodeKind.UnitExpression)
                return new TypedNopStatement(node.Syntax);

            return base.RewriteStatement(new TypedExpressionStatement(node.Syntax, expression));
        }

        return node switch
        {
            TypedVariableDeclarationStatement(_, var variable, var inner)
                when variable.Type == Type.Unit
                => inner == null ? new TypedNopStatement(node.Syntax) : RewriteSubExpression(inner),
            TypedAssignmentStatement(_, var variable, var inner) when variable.Type == Type.Unit
                => RewriteSubExpression(inner),
            TypedExpressionStatement(_, var inner) when inner.Type == Type.Unit
                => RewriteSubExpression(inner),
            _ => base.RewriteStatement(node)
        };
    }

    protected override TypedExpression RewriteExpression(TypedExpression node)
    {
        if (
            node is TypedVariableExpression variableExpression
            && variableExpression.Type == Type.Unit
        )
            return new TypedUnitExpression(node.Syntax);

        if (
            node is TypedAssignmentExpression(_, var variable, var inner)
            && variable.Type == Type.Unit
        )
        {
            var expression = RewriteExpression(inner);

            return base.RewriteExpression(expression);
        }

        return base.RewriteExpression(node);
    }

    public static TypedBlockExpression Lower(TypedBlockExpression blockExpression) =>
        new RemoveUnitAssignments().Rewrite(blockExpression);
}
