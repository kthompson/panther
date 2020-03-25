﻿using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    public sealed class AssignmentStatementSyntax : StatementSyntax
    {
        public SyntaxToken ValOrVarToken { get; }
        public SyntaxToken IdentifierToken { get; }
        public SyntaxToken EqualsToken { get; }
        public ExpressionSyntax Expression { get; }
        public override SyntaxKind Kind => SyntaxKind.AssignmentStatement;

        public AssignmentStatementSyntax(SyntaxToken valOrVarToken, SyntaxToken identifierToken, SyntaxToken equalsToken, ExpressionSyntax expression)
        {
            ValOrVarToken = valOrVarToken;
            IdentifierToken = identifierToken;
            EqualsToken = equalsToken;
            Expression = expression;
        }
    }
}