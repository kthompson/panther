namespace Panther.CodeAnalysis.Syntax
{
    internal class UnitExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenParenToken { get; }
        public SyntaxToken CloseParenToken { get; }

        public UnitExpressionSyntax(SyntaxToken openParenToken, SyntaxToken closeParenToken)
        {
            OpenParenToken = openParenToken;
            CloseParenToken = closeParenToken;
        }

        public override SyntaxKind Kind => SyntaxKind.UnitExpression;
    }
}