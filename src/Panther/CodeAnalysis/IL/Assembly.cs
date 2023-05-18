using System.Collections.Immutable;

namespace Panther.CodeAnalysis.IL;

record Assembly(
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<AssemblyClassDeclaration> ClassDeclarations
);
