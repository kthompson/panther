namespace Panther.CodeAnalysis.Syntax
{
    public sealed class BreakExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken BreakKeyword { get; }
        public BreakExpressionSyntax(SyntaxToken breakKeyword)
        {
            BreakKeyword = breakKeyword;
        }

        public override SyntaxKind Kind => SyntaxKind.BreakExpression;
    }
}