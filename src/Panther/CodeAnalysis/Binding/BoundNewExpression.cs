using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundNewExpression(SyntaxNode Syntax, Symbol Constructor, ImmutableArray<BoundExpression> Arguments) : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.NewExpression;
        public override Type Type { get ; init; } = Constructor.Owner.Type;
    }
}