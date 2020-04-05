namespace Panther.CodeAnalysis.Syntax
{
    public sealed class ExpressionStatementSyntax : StatementSyntax
    {
        public ExpressionSyntax Expression { get; }
        public SyntaxToken? NewLineToken { get; }
        public override SyntaxKind Kind => SyntaxKind.ExpressionStatement;

        public ExpressionStatementSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression, SyntaxToken? newLineToken) : base(syntaxTree)
        {
            Expression = expression;
            NewLineToken = newLineToken;
        }
    }
}