using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public partial class GroupExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenParenToken { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken CloseParenToken { get; }

        public GroupExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken openParenToken, ExpressionSyntax expression, SyntaxToken closeParenToken) : base(syntaxTree)
        {
            OpenParenToken = openParenToken;
            Expression = expression;
            CloseParenToken = closeParenToken;
        }
    }
}