using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal abstract class BoundLoopExpression : BoundExpression
    {
        public BoundLabel BreakLabel { get; }
        public BoundLabel ContinueLabel { get; }

        protected BoundLoopExpression(SyntaxNode syntax, BoundLabel breakLabel, BoundLabel continueLabel)
            : base(syntax)
        {
            BreakLabel = breakLabel;
            ContinueLabel = continueLabel;
        }
    }
}