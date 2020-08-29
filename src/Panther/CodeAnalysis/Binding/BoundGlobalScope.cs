using System.Collections.Immutable;
using Mono.Cecil;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    /// <summary>
    /// Builds all of the syntax trees into the parts that will eventually
    /// become a BoundAssembly
    /// </summary>
    internal sealed class BoundGlobalScope
    {
        public BoundGlobalScope? Previous { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        public EntryPoint? EntryPoint { get; }

        /// <summary>
        /// The type that contains the top level statements
        /// </summary>
        public BoundType? DefaultType { get; }
        public ImmutableArray<BoundType> Types { get; }
        public ImmutableArray<AssemblyDefinition> References { get; }

        public BoundGlobalScope(BoundGlobalScope? previous, ImmutableArray<Diagnostic> diagnostics,
            BoundType? defaultType,
            EntryPoint? entryPoint,
            ImmutableArray<BoundType> types,
            ImmutableArray<AssemblyDefinition> references)
        {
            Previous = previous;
            Diagnostics = diagnostics;
            EntryPoint = entryPoint;
            References = references;
            DefaultType = defaultType;
            Types = types;
        }
    }
}