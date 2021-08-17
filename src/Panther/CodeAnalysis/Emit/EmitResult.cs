using System.Collections.Generic;
using System.Collections.Immutable;
using Mono.Cecil;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Emit
{
    public sealed class EmitResult : IEmitResult
    {
        internal EmitResult(ImmutableArray<Diagnostic> diagnostics,
            Dictionary<Symbol, FieldReference> globals, Dictionary<Symbol, MethodReference> methods,
            AssemblyDefinition? assembly, string? outputPath)
        {
            Diagnostics = diagnostics;
            Globals = globals;
            Assembly = assembly;
            OutputPath = outputPath;
            Methods = methods;
        }

        public string? OutputPath { get; }
        public ImmutableArray<Diagnostic> Diagnostics { get; }
        internal Dictionary<Symbol, FieldReference> Globals { get; }
        internal Dictionary<Symbol, MethodReference> Methods { get; }
        internal AssemblyDefinition? Assembly { get; }
    }
}