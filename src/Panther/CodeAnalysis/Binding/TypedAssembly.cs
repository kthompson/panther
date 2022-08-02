using System;
using System.Collections.Immutable;
using Mono.Cecil;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding;

internal sealed record TypedAssembly(
    TypedAssembly? Previous,
    ImmutableArray<Diagnostic> Diagnostics,
    EntryPoint? EntryPoint,
    TypedType? DefaultType,
    Symbol RootSymbol,
    ImmutableDictionary<Symbol, TypedBlockExpression> MethodDefinitions,
    ImmutableArray<AssemblyDefinition> References
);
