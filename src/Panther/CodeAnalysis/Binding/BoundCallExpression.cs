using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundCallExpression(SyntaxNode Syntax, MethodSymbol Method, ImmutableArray<BoundExpression> Arguments) : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type { get ; init; } = Method.ReturnType;
    }
}