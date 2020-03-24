using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public BoundExpression Expression { get; }

        public BoundGlobalScope(BoundGlobalScope previous, ImmutableArray<Diagnostic> diagnostics, ImmutableArray<VariableSymbol> variables, BoundExpression expression)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            Variables = variables;
            Expression = expression;
        }
    }
}