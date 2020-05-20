using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public MethodSymbol Method { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Method.ReturnType;

        public BoundCallExpression(MethodSymbol method, ImmutableArray<BoundExpression> arguments)
        {
            Method = method;
            Arguments = arguments;
        }
    }
}