using System;
using System.Collections.Immutable;
using Mono.Cecil;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundAssembly(
        BoundAssembly? Previous,
        ImmutableArray<Diagnostic> Diagnostics,
        EntryPoint? EntryPoint,
        ImmutableArray<BoundType> Types,
        ImmutableArray<AssemblyDefinition> References
    );
}