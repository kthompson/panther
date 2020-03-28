using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed class VariableDeclarationStatementSyntax : StatementSyntax
    {
        public SyntaxToken ValOrVarToken { get; }
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }
        public SyntaxToken NewLineToken { get; }
        public override SyntaxKind Kind => SyntaxKind.VariableDeclarationStatement;

        public VariableDeclarationStatementSyntax(SyntaxToken valOrVarToken, SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression, SyntaxToken newLineToken)
        {
            ValOrVarToken = valOrVarToken;
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
            NewLineToken = newLineToken;
        }
    }
}