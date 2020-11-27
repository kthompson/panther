using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    sealed record BoundLabelStatement : BoundStatement
    {
        public BoundLabel BoundLabel { get; }

        public BoundLabelStatement(SyntaxNode syntax, BoundLabel boundLabel) : base(syntax)
        {
            BoundLabel = boundLabel;
        }

        public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;
    }
}