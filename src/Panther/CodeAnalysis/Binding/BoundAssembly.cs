using System;
using System.Collections.Immutable;
using Mono.Cecil;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundAssembly
    {
        public BoundAssembly? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public EntryPoint? EntryPoint { get; }
        public ImmutableArray<BoundType> Types { get; }
        public ImmutableArray<AssemblyDefinition> References { get; }

        public BoundAssembly(BoundAssembly? previous, ImmutableArray<Diagnostic> diagnostics, EntryPoint? entryPoint, ImmutableArray<AssemblyDefinition> references, ImmutableArray<BoundType> types)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            EntryPoint = entryPoint;
            References = references;
            Types = types;
        }
    }
}