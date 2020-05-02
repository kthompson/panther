namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class CallExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken OpenParenToken { get; }
        public SeparatedSyntaxList<ExpressionSyntax> Arguments { get; }
        public SyntaxToken CloseParenToken { get; }

        public CallExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken openParenToken, SeparatedSyntaxList<ExpressionSyntax> arguments, SyntaxToken closeParenToken) : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            OpenParenToken = openParenToken;
            Arguments = arguments;
            CloseParenToken = closeParenToken;
        }
    }
}