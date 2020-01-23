using System.Collections.Generic;

namespace Panther.CodeAnalysis
{
    internal class GroupExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenParenToken { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken CloseParenToken { get; }
        public override SyntaxKind Kind => SyntaxKind.GroupExpression;

        public GroupExpressionSyntax(SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken)
        {
            OpenParenToken = openParenToken;
            Expression = expression;
            CloseParenToken = closeParenToken;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return OpenParenToken;
            yield return Expression;
            yield return CloseParenToken;
        }
    }
}