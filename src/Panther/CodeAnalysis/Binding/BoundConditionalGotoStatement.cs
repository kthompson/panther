using System;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

sealed partial record BoundConditionalGotoStatement
{
    public BoundConditionalGotoStatement(SyntaxNode syntax, BoundLabel boundLabel,
        BoundExpression condition) : this(syntax, boundLabel, condition, false)
    {

    }
}