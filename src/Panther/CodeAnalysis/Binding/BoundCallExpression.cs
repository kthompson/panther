using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public FunctionSymbol Function { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Function.ReturnType;

        public BoundCallExpression(FunctionSymbol function, ImmutableArray<BoundExpression> arguments)
        {
            Function = function;
            Arguments = arguments;
        }
    }
}