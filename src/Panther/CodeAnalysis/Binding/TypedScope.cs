using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding;

internal sealed class TypedScope : SymbolContainer
{
    public Symbol Symbol { get; }

    // symbols that have not been defined but are needed in scope for resolving types etc
    private readonly Dictionary<string, ImmutableArray<Symbol>> _importedSymbols = new();

    public TypedScope Parent { get; }

    public TypedScope(TypedScope parent, string? name = null) : this(parent.Symbol, parent, name)
    { }

    public TypedScope(Symbol symbol, string? name = null) : this(symbol, null, name) { }

    public TypedScope(TypedScope parent, Symbol symbol, string? name = null)
        : this(symbol, parent, name) { }

    private TypedScope(Symbol symbol, TypedScope? parent, string? name = null)
    {
        this.Name = name ?? symbol.Name;
        Symbol = symbol;
        Parent = parent ?? this;
    }

    public override string Name { get; }
    public bool IsRootScope => Parent == this;
    public bool IsGlobalScope => Symbol.IsType && Symbol.Name == "$Program";

    public void ImportMembers(Symbol namespaceOrTypeSymbol)
    {
        foreach (var member in namespaceOrTypeSymbol.Members)
        {
            Import(member);
        }
    }

    public void Import(Symbol symbol)
    {
        base.DefineSymbol(symbol);
    }

    public override bool DefineSymbol(Symbol symbol)
    {
        return Symbol.DefineSymbol(symbol);
    }

    public override Symbol? LookupSingle(string name, Predicate<Symbol> predicate)
    {
        var symbol = Symbol.LookupSingle(name, predicate);

        if (symbol != null)
            return symbol;

        var import = base.LookupSingle(name, predicate);
        if (import != null)
            return import;

        if (IsRootScope)
            return null;

        return Parent.LookupSingle(name, predicate);
    }

    public ImmutableArray<Symbol> LookupSymbol(string name, bool deep = true)
    {
        var members = Symbol.LookupMembers(name).ToImmutableArray();
        if (members.Any())
            return members;

        var imports = base.LookupMembers(name);
        if (!imports.IsEmpty)
            return imports;

        if (!deep)
            return ImmutableArray<Symbol>.Empty;

        if (IsRootScope)
            return ImmutableArray<Symbol>.Empty;

        return Parent.LookupSymbol(name, deep);
    }

    // public ImmutableArray<Symbol> LookupMethod(string name, bool deep = true)
    // {
    //     var methods = Symbol.LookupMembers(name).Where(m => m.IsMethod).ToImmutableArray();
    //
    //     if (methods.Any())
    //         return methods;
    //
    //     if (_importedSymbols.TryGetValue(name, out var symbols))
    //     {
    //         return symbols.Where(m => m.IsMethod).ToImmutableArray();
    //     }
    //
    //     if (!deep)
    //         return ImmutableArray<Symbol>.Empty;
    //
    //     if (IsRootScope)
    //         return ImmutableArray<Symbol>.Empty;
    //
    //     return Parent.LookupMethod(name, deep);
    // }
}
