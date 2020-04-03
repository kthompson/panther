﻿using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundProgram
    {
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public ImmutableDictionary<FunctionSymbol, BoundExpression> Functions { get; }
        public BoundBlockExpression Expression { get; }

        public BoundProgram(ImmutableArray<Diagnostic> diagnostics, ImmutableDictionary<FunctionSymbol, BoundExpression> functions, BoundBlockExpression expression)
        {
            Diagnostics = diagnostics;
            Functions = functions;
            Expression = expression;
        }
    }
}