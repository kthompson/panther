using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed class AssignmentExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken ValToken { get; }
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }
        public override SyntaxKind Kind => SyntaxKind.AssignmentExpression;

        public AssignmentExpressionSyntax(SyntaxToken valToken, SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression)
        {
            ValToken = valToken;
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
        }

        public override IEnumerable<SyntaxNode> GetChildren()
        {
            yield return ValToken;
            yield return IdentifierToken;
            yield return EqualsToken;
            yield return Expression;
        }
    }
}