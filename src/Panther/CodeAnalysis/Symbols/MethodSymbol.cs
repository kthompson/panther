﻿using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class MethodSymbol : Symbol
    {
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }

        protected MethodSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType)
            : base(name)
        {
            Parameters = parameters;
            ReturnType = returnType;
        }
    }
}