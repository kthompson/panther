namespace Panther.CodeAnalysis.Syntax
{
    public sealed class ContinueExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken ContinueKeyword { get; }

        public ContinueExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken continueKeyword) : base(syntaxTree)
        {
            ContinueKeyword = continueKeyword;
        }

        public override SyntaxKind Kind => SyntaxKind.ContinueExpression;
    }
}