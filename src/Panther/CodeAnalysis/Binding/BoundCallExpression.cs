using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundCallExpression : BoundExpression
    {
        public MethodSymbol Method { get; }
        public ImmutableArray<BoundExpression> Arguments { get; }
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override TypeSymbol Type => Method.ReturnType;

        public BoundCallExpression(SyntaxNode syntax, MethodSymbol method, ImmutableArray<BoundExpression> arguments)
            : base(syntax)
        {
            Method = method;
            Arguments = arguments;
        }
    }
}