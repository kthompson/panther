namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class ForExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken ForKeyword { get; }
        public SyntaxToken OpenParenToken { get; }
        public SyntaxToken Variable { get; }
        public SyntaxToken LessThanDashToken { get; }
        public ExpressionSyntax FromExpression { get; }
        public SyntaxToken ToKeyword { get; }
        public ExpressionSyntax ToExpression { get; }
        public SyntaxToken CloseParenToken { get; }
        public ExpressionSyntax Body { get; }

        public ForExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken forKeyword, SyntaxToken openParenToken, SyntaxToken variable, SyntaxToken lessThanDashToken, ExpressionSyntax fromExpression, SyntaxToken toKeyword, ExpressionSyntax toExpression, SyntaxToken closeParenToken, ExpressionSyntax body) : base(syntaxTree)
        {
            ForKeyword = forKeyword;
            OpenParenToken = openParenToken;
            Variable = variable;
            LessThanDashToken = lessThanDashToken;
            FromExpression = fromExpression;
            ToKeyword = toKeyword;
            ToExpression = toExpression;
            CloseParenToken = closeParenToken;
            Body = body;
        }
    }
}