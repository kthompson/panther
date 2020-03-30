namespace Panther.CodeAnalysis.Binding
{
    sealed class BoundLabelStatement : BoundStatement
    {
        public BoundLabel BoundLabel { get; }

        public BoundLabelStatement(BoundLabel boundLabel)
        {
            BoundLabel = boundLabel;
        }

        public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;
    }
}