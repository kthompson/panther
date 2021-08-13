using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal record BoundBlockExpression(SyntaxNode Syntax, ImmutableArray<BoundStatement> Statements, BoundExpression Expression)
        : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.BlockExpression;
        public override Type Type { get ; init; } = Expression.Type;
    }
}