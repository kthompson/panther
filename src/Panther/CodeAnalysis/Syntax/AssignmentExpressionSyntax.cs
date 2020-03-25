using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed class AssignmentStatementSyntax : StatementSyntax
    {
        public SyntaxToken ValToken { get; }
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }
        public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;

        public AssignmentStatementSyntax(SyntaxToken valToken, SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression)
        {
            ValToken = valToken;
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
        }
    }
}