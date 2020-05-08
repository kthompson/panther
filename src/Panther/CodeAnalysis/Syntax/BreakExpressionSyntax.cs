namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class BreakExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken BreakKeyword { get; }

        public BreakExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken breakKeyword) : base(syntaxTree)
        {
            BreakKeyword = breakKeyword;
        }
    }
}