namespace Panther.CodeAnalysis.Syntax
{
    public sealed class AssignmentStatementSyntax : StatementSyntax
    {
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken NewLineToken { get; }
        public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;

        public AssignmentStatementSyntax(SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression, SyntaxToken newLineToken)
        {
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
            NewLineToken = newLineToken;
        }
    }
}