using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    sealed record BoundGotoStatement : BoundStatement
    {
        public BoundLabel BoundLabel { get; }

        public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;

        public BoundGotoStatement(SyntaxNode syntax, BoundLabel boundLabel) : base(syntax)
        {
            BoundLabel = boundLabel;
        }
    }
}