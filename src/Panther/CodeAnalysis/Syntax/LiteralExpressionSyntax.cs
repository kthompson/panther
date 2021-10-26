using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial record LiteralExpressionSyntax : ExpressionSyntax
    {
        public override SyntaxKind Kind => SyntaxKind.LiteralExpression;

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return LiteralToken;
        }

        public override void Accept(SyntaxVisitor visitor)
        {
            visitor.VisitLiteralExpression(this);
        }

        public override TResult Accept<TResult>(SyntaxVisitor<TResult> visitor) =>
            visitor.VisitLiteralExpression(this);


        public LiteralExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken literalToken)
            : this(syntaxTree, literalToken, literalToken.Value)
        {
        }

        public LiteralExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken literalToken, object? value) : base(syntaxTree)
        {
            LiteralToken = literalToken;
            Value = value;
        }

        public SyntaxToken LiteralToken { get; }
        public object? Value { get; }
    }
}