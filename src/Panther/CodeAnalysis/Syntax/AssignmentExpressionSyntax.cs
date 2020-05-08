namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }

        public AssignmentExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression) : base(syntaxTree)
        {
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
        }
    }
}