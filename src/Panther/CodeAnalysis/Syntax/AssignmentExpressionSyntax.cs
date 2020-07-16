namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Name { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }

        public AssignmentExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax name, SyntaxToken equalsToken, ExpressionSyntax expression) : base(syntaxTree)
        {
            Name = name;
            EqualsToken = equalsToken;
            Expression = expression;
        }
    }
}