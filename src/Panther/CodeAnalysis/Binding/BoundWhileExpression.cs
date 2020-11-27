using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal record BoundWhileExpression(SyntaxNode Syntax, BoundExpression Condition, BoundExpression Body,
            BoundLabel BreakLabel,
            BoundLabel ContinueLabel)
        : BoundLoopExpression(Syntax, BreakLabel, ContinueLabel)
    {
        public override BoundNodeKind Kind => BoundNodeKind.WhileExpression;
        public override TypeSymbol Type { get; init; } = Body.Type;
    }
}