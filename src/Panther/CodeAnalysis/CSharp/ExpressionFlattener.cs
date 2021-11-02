using System.Collections.Generic;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.CSharp
{
    internal class ExpressionFlattener : SyntaxVisitor<SyntaxNode>
    {
        protected readonly List<StatementSyntax> _statements = new List<StatementSyntax>();

        private ExpressionFlattener()
        {
        }

        protected override SyntaxNode DefaultVisit(SyntaxNode node)
        {
            return node;
        }

        public static (ImmutableArray<StatementSyntax> statments, ExpressionSyntax expression) Flatten(ExpressionSyntax node)
        {
            var visitor = new ExpressionFlattener();
            var expression = (ExpressionSyntax)node.Accept(visitor);

            return (visitor._statements.ToImmutableArray(), expression);
        }

        public override SyntaxNode VisitBlockExpression(BlockExpressionSyntax node)
        {
            foreach (var statement in node.Statements)
            {
                statement.Accept(this);
            }

            return node.Expression.Accept(this);
        }

        public override SyntaxNode VisitExpressionStatement(ExpressionStatementSyntax node)
        {
            var result = (ExpressionStatementSyntax)base.VisitExpressionStatement(node);
            if(result.Expression.Kind != SyntaxKind.UnitExpression)
            {
                _statements.Add(result);
            }
            return result;
        }

        public override SyntaxNode VisitVariableDeclarationStatement(VariableDeclarationStatementSyntax node)
        {
            var result = (StatementSyntax)base.VisitVariableDeclarationStatement(node);
            _statements.Add(result);
            return result;
        }
    }
}