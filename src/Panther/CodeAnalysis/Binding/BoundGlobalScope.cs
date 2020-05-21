﻿using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public MethodSymbol? MainFunction { get; }
        public MethodSymbol? ScriptFunction { get; }

        public ImmutableArray<TypeSymbol> Types { get; }
        public ImmutableArray<MethodSymbol> Functions { get; }
        public ImmutableArray<VariableSymbol> Variables { get; }
        public ImmutableArray<BoundStatement> Statements { get; }

        public BoundGlobalScope(BoundGlobalScope? previous, ImmutableArray<Diagnostic> diagnostics,
            MethodSymbol? mainFunction, MethodSymbol? scriptFunction, ImmutableArray<VariableSymbol> variables,
            ImmutableArray<TypeSymbol> types,
            ImmutableArray<MethodSymbol> functions, ImmutableArray<BoundStatement> statements)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            MainFunction = mainFunction;
            ScriptFunction = scriptFunction;
            Variables = variables;
            Statements = statements;
            Types = types;
            Functions = functions;
        }
    }
}