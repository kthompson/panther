namespace Panther.CodeAnalysis.Syntax
{
    public class IfExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IfKeyword { get; }
        public SyntaxToken OpenParenToken { get; }
        public ExpressionSyntax ConditionExpression { get; }
        public SyntaxToken CloseParenToken { get; }
        public ExpressionSyntax ThenExpression { get; }
        public SyntaxToken ElseKeyword { get; }
        public ExpressionSyntax ElseExpression { get; }

        public IfExpressionSyntax(SyntaxToken ifKeyword, SyntaxToken openParenToken, ExpressionSyntax conditionExpression, SyntaxToken closeParenToken, ExpressionSyntax thenExpression, SyntaxToken elseKeyword, ExpressionSyntax elseExpression)
        {
            IfKeyword = ifKeyword;
            OpenParenToken = openParenToken;
            ConditionExpression = conditionExpression;
            CloseParenToken = closeParenToken;
            ThenExpression = thenExpression;
            ElseKeyword = elseKeyword;
            ElseExpression = elseExpression;
        }

        public override SyntaxKind Kind => SyntaxKind.IfExpression;
    }
}