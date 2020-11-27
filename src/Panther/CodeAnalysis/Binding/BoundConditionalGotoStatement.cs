using System;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    sealed record BoundConditionalGotoStatement(SyntaxNode Syntax,
        BoundLabel BoundLabel ,
        BoundExpression Condition, bool JumpIfTrue = false) : BoundStatement(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;
    }
}