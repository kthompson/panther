using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Lowering
{
    sealed class RemoveUnitAssignments : BoundStatementListRewriter
    {
        private RemoveUnitAssignments()
        {
        }


        protected override BoundStatement RewriteStatement(BoundStatement node)
        {
            if (node is BoundVariableDeclarationStatement varDecl && varDecl.Variable.Type == Type.Unit)
            {
                var expression = RewriteExpression(varDecl.Expression);
                if (expression.Kind == BoundNodeKind.UnitExpression)
                    return new BoundNopStatement(node.Syntax);

                return base.RewriteStatement(new BoundExpressionStatement(node.Syntax, expression));
            }

            if (node is BoundExpressionStatement expressionStatement && expressionStatement.Expression.Type == Type.Unit)
            {
                var expression = RewriteExpression(expressionStatement.Expression);
                if (expression.Kind == BoundNodeKind.UnitExpression)
                    return new BoundNopStatement(node.Syntax);

                return base.RewriteStatement(new BoundExpressionStatement(node.Syntax, expression));
            }

            return base.RewriteStatement(node);
        }

        protected override BoundExpression RewriteExpression(BoundExpression node)
        {
            if (node is BoundVariableExpression variableExpression && variableExpression.Type == Type.Unit)
                return new BoundUnitExpression(node.Syntax);

            return base.RewriteExpression(node);
        }

        public static BoundBlockExpression Lower(BoundBlockExpression blockExpression) =>
            new RemoveUnitAssignments().Rewrite(blockExpression);
    }
}