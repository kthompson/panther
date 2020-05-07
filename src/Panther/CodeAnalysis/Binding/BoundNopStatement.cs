namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundNopStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.NopStatement;

        public static readonly BoundNopStatement Default = new BoundNopStatement();

        private BoundNopStatement()
        {
        }
    }
}