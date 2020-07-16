namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class CallExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Expression { get; }
        public SyntaxToken OpenParenToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParenToken { get; }

        public CallExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression, SyntaxToken openParenToken, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeParenToken) : base(syntaxTree)
        {
            Expression = expression;
            OpenParenToken = openParenToken;
            Arguments = arguments;
            CloseParenToken = closeParenToken;
        }
    }
}