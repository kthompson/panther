using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public class GroupExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenParenToken { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken CloseParenToken { get; }
        public override SyntaxKind Kind => SyntaxKind.GroupExpression;

        public GroupExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken) : base(syntaxTree)
        {
            OpenParenToken = openParenToken;
            Expression = expression;
            CloseParenToken = closeParenToken;
        }
    }
}