namespace Panther.CodeAnalysis.Syntax
{
    public sealed class ContinueExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken ContinueKeyword { get; }

        public ContinueExpressionSyntax(SyntaxToken continueKeyword)
        {
            ContinueKeyword = continueKeyword;
        }

        public override SyntaxKind Kind => SyntaxKind.ContinueExpression;
    }
}