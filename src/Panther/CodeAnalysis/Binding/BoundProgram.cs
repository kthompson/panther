using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockExpression> Functions { get; }
        public BoundBlockExpression Expression { get; }

        public BoundProgram(BoundProgram previous, ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FunctionSymbol, BoundBlockExpression> functions, BoundBlockExpression expression)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Functions = functions;
            Expression = expression;
        }
    }
}