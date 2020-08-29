using System.Collections.Generic;
using System.Collections.Immutable;
using Mono.Cecil;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Emit
{
    public sealed class EmitResult
    {
        internal EmitResult(ImmutableArray<Diagnostic> diagnostics,
            Dictionary<FieldSymbol, FieldReference> globals, Dictionary<MethodSymbol, MethodReference> methods,
            AssemblyDefinition? assembly)
        {
            Diagnostics = diagnostics;
            Globals = globals;
            Assembly = assembly;
            Methods = methods;
        }

        public ImmutableArray<Diagnostic> Diagnostics { get; }
        internal Dictionary<FieldSymbol, FieldReference> Globals { get; }
        internal Dictionary<MethodSymbol, MethodReference> Methods { get; }
        internal AssemblyDefinition? Assembly { get; }
    }
}