using System.Collections.Immutable;

namespace Panther.CodeAnalysis.IL;

record AssemblyClassDeclaration(
    string Name,
    ImmutableArray<AssemblyFieldDeclaration> FieldDeclarations,
    ImmutableArray<AssemblyMethodDeclaration> MethodDeclarations
);
