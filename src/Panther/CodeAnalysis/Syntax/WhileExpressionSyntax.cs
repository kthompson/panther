namespace Panther.CodeAnalysis.Syntax
{
    public class WhileExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken WhileKeyword { get; }
        public SyntaxToken OpenParenToken { get; }
        public ExpressionSyntax ConditionExpression { get; }
        public SyntaxToken CloseParenToken { get; }
        public ExpressionSyntax Expression { get; }

        public WhileExpressionSyntax(SyntaxToken whileKeyword, SyntaxToken openParenToken, ExpressionSyntax conditionExpression, SyntaxToken closeParenToken, ExpressionSyntax expression)
        {
            WhileKeyword = whileKeyword;
            OpenParenToken = openParenToken;
            ConditionExpression = conditionExpression;
            CloseParenToken = closeParenToken;
            Expression = expression;
        }

        public override SyntaxKind Kind => SyntaxKind.WhileExpression;
    }
}