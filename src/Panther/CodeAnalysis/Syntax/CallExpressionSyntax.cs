namespace Panther.CodeAnalysis.Syntax
{
    public class CallExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken OpenParenToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParenToken { get; }
        public override SyntaxKind Kind => SyntaxKind.CallExpression;

        public CallExpressionSyntax(SyntaxToken identifierToken, SyntaxToken openParenToken, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeParenToken)
        {
            IdentifierToken = identifierToken;
            OpenParenToken = openParenToken;
            Arguments = arguments;
            CloseParenToken = closeParenToken;
        }
    }
}