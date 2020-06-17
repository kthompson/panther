using System.Collections.Immutable;
using Mono.Cecil;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public BoundProgram? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public MethodSymbol? MainFunction { get; }
        public MethodSymbol? ScriptFunction { get; }
        public ImmutableDictionary<MethodSymbol, BoundBlockExpression> Functions { get; }
        public ImmutableArray<AssemblyDefinition> References { get; }

        public BoundProgram(BoundProgram? previous, ImmutableArray<Diagnostic> diagnostics, MethodSymbol? mainFunction, MethodSymbol? scriptFunction,
            ImmutableDictionary<MethodSymbol, BoundBlockExpression> functions, ImmutableArray<AssemblyDefinition> references)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            ScriptFunction = scriptFunction;
            Functions = functions;
            References = references;
        }
    }
}