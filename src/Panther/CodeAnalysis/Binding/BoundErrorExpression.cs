using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundErrorExpression : BoundExpression
    {
        public BoundErrorExpression(SyntaxNode syntax)
            : base(syntax)
        {
        }

        public override BoundNodeKind Kind => BoundNodeKind.ErrorExpression;
        public override TypeSymbol Type { get ; init; } = TypeSymbol.Error;
    }
}