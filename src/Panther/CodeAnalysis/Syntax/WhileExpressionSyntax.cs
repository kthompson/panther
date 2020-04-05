namespace Panther.CodeAnalysis.Syntax
{
    public class WhileExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken WhileKeyword { get; }
        public SyntaxToken OpenParenToken { get; }
        public ExpressionSyntax ConditionExpression { get; }
        public SyntaxToken CloseParenToken { get; }
        public ExpressionSyntax Body { get; }

        public WhileExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken whileKeyword, SyntaxToken openParenToken,
            ExpressionSyntax conditionExpression, SyntaxToken closeParenToken, ExpressionSyntax body) : base(syntaxTree)
        {
            WhileKeyword = whileKeyword;
            OpenParenToken = openParenToken;
            ConditionExpression = conditionExpression;
            CloseParenToken = closeParenToken;
            Body = body;
        }

        public override SyntaxKind Kind => SyntaxKind.WhileExpression;
    }
}