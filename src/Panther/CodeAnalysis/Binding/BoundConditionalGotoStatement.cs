namespace Panther.CodeAnalysis.Binding
{
    sealed class BoundConditionalGotoStatement : BoundStatement
    {
        public BoundLabel BoundLabel { get; }
        public BoundExpression Condition { get; }
        public bool JumpIfFalse { get; }

        public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;

        public BoundConditionalGotoStatement(BoundLabel boundLabel, BoundExpression condition, bool jumpIfFalse = false)
        {
            BoundLabel = boundLabel;
            Condition = condition;
            JumpIfFalse = jumpIfFalse;
        }
    }
}