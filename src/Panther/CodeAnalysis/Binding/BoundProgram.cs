using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? MainFunction { get; }
        public FunctionSymbol? ScriptFunction { get; }
        public ImmutableDictionary<FunctionSymbol, BoundBlockExpression> Functions { get; }

        public BoundProgram(BoundProgram previous, ImmutableArray<Diagnostic> diagnostics, FunctionSymbol? mainFunction, FunctionSymbol? scriptFunction,
            ImmutableDictionary<FunctionSymbol, BoundBlockExpression> functions)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            ScriptFunction = scriptFunction;
            Functions = functions;
        }
    }
}