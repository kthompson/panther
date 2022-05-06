using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Panther.CodeAnalysis.Symbols;

public abstract class SymbolContainer
{
    private readonly Dictionary<string, ImmutableArray<Symbol>> _symbolMap = new();
    private readonly List<Symbol> _symbols = new();

    protected void Delete(Symbol child)
    {
        if (_symbols.Remove(child))
        {
            _symbolMap[child.Name] = _symbolMap[child.Name].Remove(child);
        }
    }

    public virtual bool DefineSymbol(Symbol symbol)
    {
        // only one field symbol per name
        if ((symbol.Flags & SymbolFlags.Field) != 0)
        {
            if (_symbolMap.ContainsKey(symbol.Name))
                return false;

            _symbolMap.Add(symbol.Name, ImmutableArray.Create(symbol));
            _symbols.Add(symbol);
            return true;
        }

        if (_symbolMap.TryGetValue(symbol.Name, out var symbols))
        {
            _symbolMap[symbol.Name] = symbols.Add(symbol);
            _symbols.Add(symbol);
            return true;
        }

        _symbolMap.Add(symbol.Name, ImmutableArray.Create(symbol));
        _symbols.Add(symbol);
        return true;
    }

    public ImmutableArray<Symbol> Locals  =>
        Members.Where(m => m.IsLocal).ToImmutableArray();

    public ImmutableArray<Symbol> Parameters =>
        Members.Where(m => m.IsParameter).ToImmutableArray();

    public ImmutableArray<Symbol> Constructors =>
        Members.Where(m => m.IsConstructor).ToImmutableArray();

    public ImmutableArray<Symbol> Methods =>
        Members.Where(sym => sym.IsMethod).ToImmutableArray();

    public ImmutableArray<Symbol> Fields =>
        Members.Where(sym => sym.IsField).ToImmutableArray();

    public ImmutableArray<Symbol> Types =>
        Members.Where(m => m.IsType).ToImmutableArray();

    public ImmutableArray<Symbol> Namespaces =>
        Members.Where(m => m.IsNamespace).ToImmutableArray();

    public virtual ImmutableArray<Symbol> Members => _symbols.ToImmutableArray();

    public virtual ImmutableArray<Symbol> LookupMembers(string name) =>
        _symbolMap.TryGetValue(name, out var symbols)
            ? symbols
            : ImmutableArray<Symbol>.Empty;

    public virtual Symbol? LookupSingle(string name, Predicate<Symbol> predicate) =>
        LookupMembers(name).SingleOrDefault(m => predicate(m));

    public virtual Symbol? LookupNamespace(string name) =>
        LookupSingle(name, m => m.IsNamespace);

    public virtual Symbol? LookupVariable(string name) =>
        LookupSingle(name, m => m.IsLocal | m.IsParameter);
    
    public virtual Symbol? LookupField(string name) =>
        LookupSingle(name, m => m.IsField);
    
    public virtual Symbol? LookupType(string name) =>
        LookupSingle(name, m => m.IsType);

    public virtual ImmutableArray<Symbol> LookupMethod(string name) =>
        LookupMembers(name).Where(m => m.IsMethod).ToImmutableArray();
}