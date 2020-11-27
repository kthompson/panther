using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundNopStatement : BoundStatement
    {
        public override BoundNodeKind Kind => BoundNodeKind.NopStatement;

        public BoundNopStatement(SyntaxNode syntax) : base(syntax)
        {
        }
    }
}