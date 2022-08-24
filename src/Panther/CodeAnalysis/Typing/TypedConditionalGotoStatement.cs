using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Typing;

sealed partial record TypedConditionalGotoStatement
{
    public TypedConditionalGotoStatement(
        SyntaxNode syntax,
        TypedLabel TypedLabel,
        TypedExpression condition
    ) : this(syntax, TypedLabel, condition, false) { }
}
