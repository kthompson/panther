using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Lowering;

sealed class RemoveUnitAssignments : BoundStatementListRewriter
{
    private RemoveUnitAssignments()
    {
    }

    protected override BoundStatement RewriteStatement(BoundStatement node)
    {
        BoundStatement RewriteSubExpression(BoundExpression original)
        {
            var expression = RewriteExpression(original);
            if (expression.Kind == BoundNodeKind.UnitExpression)
                return new BoundNopStatement(node.Syntax);

            return base.RewriteStatement(new BoundExpressionStatement(node.Syntax, expression));
        }

        return node switch
        {
            BoundVariableDeclarationStatement(_, var variable, var inner) when variable.Type == Type.Unit =>
                inner == null ? new BoundNopStatement(node.Syntax) : RewriteSubExpression(inner),
            BoundAssignmentStatement(_, var variable, var inner) when variable.Type == Type.Unit => RewriteSubExpression(inner),
            BoundExpressionStatement(_, var inner) when inner.Type == Type.Unit => RewriteSubExpression(inner),
            _ => base.RewriteStatement(node)
        };
    }

    protected override BoundExpression RewriteExpression(BoundExpression node)
    {
        if (node is BoundVariableExpression variableExpression && variableExpression.Type == Type.Unit)
            return new BoundUnitExpression(node.Syntax);

        if (node is BoundAssignmentExpression(_, var variable, var inner) && variable.Type == Type.Unit)
        {
            var expression = RewriteExpression(inner);

            return base.RewriteExpression(expression);
        }

        return base.RewriteExpression(node);
    }

    public static BoundBlockExpression Lower(BoundBlockExpression blockExpression) =>
        new RemoveUnitAssignments().Rewrite(blockExpression);
}