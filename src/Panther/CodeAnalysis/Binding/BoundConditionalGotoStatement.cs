using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    sealed class BoundConditionalGotoStatement : BoundStatement
    {
        public BoundLabel BoundLabel { get; }
        public BoundExpression Condition { get; }

        public bool JumpIfTrue { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;

        public BoundConditionalGotoStatement(SyntaxNode syntax, BoundLabel boundLabel, BoundExpression condition, bool jumpIfTrue = false)
            : base(syntax)
        {
            BoundLabel = boundLabel;
            Condition = condition;
            JumpIfTrue = jumpIfTrue;
        }
    }
}