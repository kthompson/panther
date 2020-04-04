namespace Panther.CodeAnalysis.Binding
{
    internal abstract class BoundLoopExpression : BoundExpression
    {
        public BoundLabel BreakLabel { get; }
        public BoundLabel ContinueLabel { get; }

        protected BoundLoopExpression(BoundLabel breakLabel, BoundLabel continueLabel)
        {
            BreakLabel = breakLabel;
            ContinueLabel = continueLabel;
        }
    }
}