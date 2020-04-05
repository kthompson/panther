﻿namespace Panther.CodeAnalysis.Syntax
{
    internal class UnitExpressionSyntax : ExpressionSyntax
    {
        public SyntaxToken OpenParenToken { get; }
        public SyntaxToken CloseParenToken { get; }

        public UnitExpressionSyntax(SyntaxTree syntaxTree, SyntaxToken openParenToken, SyntaxToken closeParenToken) : base(syntaxTree)
        {
            OpenParenToken = openParenToken;
            CloseParenToken = closeParenToken;
        }

        public override SyntaxKind Kind => SyntaxKind.UnitExpression;
    }
}