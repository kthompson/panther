namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class ExpressionStatementSyntax : StatementSyntax
    {
        public ExpressionSyntax Expression { get; }

        public ExpressionStatementSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression)
            : base(syntaxTree)
        {
            Expression = expression;
        }
    }
}