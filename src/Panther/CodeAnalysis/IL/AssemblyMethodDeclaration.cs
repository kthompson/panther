using System.Collections.Immutable;

namespace Panther.CodeAnalysis.IL;

record AssemblyMethodDeclaration(
    string Name,
    bool EntryPoint,
    int Arguments,
    int Locals,
    ImmutableArray<Instruction> Instructions
);
