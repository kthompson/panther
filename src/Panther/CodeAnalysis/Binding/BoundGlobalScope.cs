using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public FunctionSymbol? MainFunction { get; }
        public FunctionSymbol? ScriptFunction { get; }
        public ImmutableArray<FunctionSymbol> Functions { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public ImmutableArray<BoundStatement> Statements { get; }

        public BoundGlobalScope(BoundGlobalScope? previous, ImmutableArray<Diagnostic> diagnostics,
            FunctionSymbol? mainFunction,FunctionSymbol? scriptFunction, ImmutableArray<VariableSymbol> variables,
            ImmutableArray<FunctionSymbol> functions, ImmutableArray<BoundStatement> statements)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            ScriptFunction = scriptFunction;
            Variables = variables;
            Statements = statements;
            Functions = functions;
        }
    }
}