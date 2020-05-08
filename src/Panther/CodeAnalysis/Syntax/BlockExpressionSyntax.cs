using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed partial class BlockExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenBraceToken { get; }
        public ImmutableArray<StatementSyntax> Statements { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken CloseBraceToken { get; }

        public BlockExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken openBraceToken, ImmutableArray<StatementSyntax> statements, ExpressionSyntax expression, SyntaxToken closeBraceToken) : base(syntaxTree)
        {
            OpenBraceToken = openBraceToken;
            Statements = statements;
            Expression = expression;
            CloseBraceToken = closeBraceToken;
        }
    }
}