namespace Panther.CodeAnalysis.Binding
{
    sealed class BoundGotoStatement : BoundStatement
    {
        public LabelSymbol Label { get; }

        public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;

        public BoundGotoStatement(LabelSymbol label)
        {
            Label = label;
        }
    }
}