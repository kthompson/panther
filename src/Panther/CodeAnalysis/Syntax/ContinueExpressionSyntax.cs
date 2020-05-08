namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class ContinueExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken ContinueKeyword { get; }

        public ContinueExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken continueKeyword) : base(syntaxTree)
        {
            ContinueKeyword = continueKeyword;
        }
    }
}