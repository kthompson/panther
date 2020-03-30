namespace Panther.CodeAnalysis.Binding
{
    sealed class BoundGotoStatement : BoundStatement
    {
        public BoundLabel BoundLabel { get; }

        public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;

        public BoundGotoStatement(BoundLabel boundLabel)
        {
            BoundLabel = boundLabel;
        }
    }
}