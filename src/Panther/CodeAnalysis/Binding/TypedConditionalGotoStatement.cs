using System;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

sealed partial record TypedConditionalGotoStatement
{
    public TypedConditionalGotoStatement(
        SyntaxNode syntax,
        TypedLabel TypedLabel,
        TypedExpression condition
    ) : this(syntax, TypedLabel, condition, false) { }
}
