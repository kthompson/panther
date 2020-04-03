namespace Panther.CodeAnalysis.Syntax
{
    public sealed class ExpressionStatementSyntax : StatementSyntax
    {
        public ExpressionSyntax Expression { get; }
        public SyntaxToken? NewLineToken { get; }
        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;

        public ExpressionStatementSyntax(ExpressionSyntax expression, SyntaxToken? newLineToken)
        {
            Expression = expression;
            NewLineToken = newLineToken;
        }
    }
}