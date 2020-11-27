using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal abstract record BoundLoopExpression(SyntaxNode Syntax, BoundLabel BreakLabel, BoundLabel ContinueLabel)
        : BoundExpression(Syntax);
}