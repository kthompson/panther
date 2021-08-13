using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundCallExpression(SyntaxNode Syntax, Symbol Method, BoundExpression? Expression, ImmutableArray<BoundExpression> Arguments) : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
        public override Type Type { get ; init; } = Method.IsConstructor ? Method.Owner.Type : Method.ReturnType;
    }
}