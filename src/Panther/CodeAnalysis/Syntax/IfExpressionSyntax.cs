namespace Panther.CodeAnalysis.Syntax
{
    public class IfExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IfKeyword { get; }
        public ExpressionSyntax ConditionExpression { get; }
        public SyntaxToken ThenKeyword { get; }
        public ExpressionSyntax ThenExpression { get; }
        public SyntaxToken ElseKeyword { get; }
        public ExpressionSyntax ElseExpression { get; }

        public IfExpressionSyntax(SyntaxToken ifKeyword, ExpressionSyntax conditionExpression, SyntaxToken thenKeyword, ExpressionSyntax thenExpression, SyntaxToken elseKeyword, ExpressionSyntax elseExpression)
        {
            IfKeyword = ifKeyword;
            ConditionExpression = conditionExpression;
            ThenKeyword = thenKeyword;
            ThenExpression = thenExpression;
            ElseKeyword = elseKeyword;
            ElseExpression = elseExpression;
        }

        public override SyntaxKind Kind => SyntaxKind.IfExpression;
    }
}