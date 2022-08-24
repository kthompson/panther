using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols;

public enum SymbolKind
{
    Unknown,

    Namespace,
    Class,
    Method,
    Field,
    Parameter,
    Variable,
    Value,
}

public interface INamespaceOrTypeSymbol : ISymbol
{
    ImmutableArray<ISymbol> GetMembers();
    ImmutableArray<ISymbol> GetMembers(string name);
    ImmutableArray<ITypeSymbol> GetTypeMembers();
    ImmutableArray<ITypeSymbol> GetTypeMembers(string name);
}

public interface ITypeSymbol : INamespaceOrTypeSymbol { }

public interface INamespaceSymbol : INamespaceOrTypeSymbol
{
    new ImmutableArray<INamespaceOrTypeSymbol> GetMembers();
    new ImmutableArray<INamespaceOrTypeSymbol> GetMembers(string name);
    ImmutableArray<INamespaceSymbol> GetNamespaceMembers();
    bool IsGlobalNamespace { get; }
}

public interface ISymbol
{
    SymbolKind Kind { get; }

    string Name { get; }
}
