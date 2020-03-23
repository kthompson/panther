﻿using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    internal sealed class LiteralExpressionSyntax : ExpressionSyntax
    {
        public LiteralExpressionSyntax(SyntaxToken literalToken)
            : this(literalToken, literalToken.Value)
        {
        }

        public LiteralExpressionSyntax(SyntaxToken literalToken, object value)
        {
            LiteralToken = literalToken;
            Value = value;
        }

        public SyntaxToken LiteralToken { get; }
        public object Value { get; }
        public override SyntaxKind Kind => SyntaxKind.LiteralExpression;
    }
}