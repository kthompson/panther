using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Syntax
{
    public class BlockExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenBraceToken { get; }
        public ImmutableArray<StatementSyntax> Statements { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken CloseBraceToken { get; }
        public override SyntaxKind Kind => SyntaxKind.BlockExpression;

        public BlockExpressionSyntax(SyntaxToken openBraceToken, ImmutableArray<StatementSyntax> statements, ExpressionSyntax expression, SyntaxToken closeBraceToken)
        {
            OpenBraceToken = openBraceToken;
            Statements = statements;
            Expression = expression;
            CloseBraceToken = closeBraceToken;
        }
    }
}