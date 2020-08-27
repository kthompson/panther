namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class MemberAccessExpressionSyntax : ExpressionSyntax
    {
        public ExpressionSyntax Expression { get; }
        public SyntaxToken DotToken { get; }
        public IdentifierNameSyntax Name { get; }
        public MemberAccessExpressionSyntax(SyntaxTree syntaxTree, ExpressionSyntax expression, SyntaxToken dotToken,
            IdentifierNameSyntax name) : base(syntaxTree)
        {
            Expression = expression;
            Name = name;
            DotToken = dotToken;
        }
    }
}