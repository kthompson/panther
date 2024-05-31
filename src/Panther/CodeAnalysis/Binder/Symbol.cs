using System.Collections;
using System.Collections.Generic;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Binder;

public class Symbol(string name, SymbolFlags flags, TextLocation location, Symbol? parent)
    : IEnumerable<Symbol>
{
    private Dictionary<string, Symbol>? _symbols;
    private List<Symbol>? _symbolList;

    public static Symbol NewRoot() => new("", SymbolFlags.None, TextLocation.None, null);

    public string Name => name;
    public SymbolFlags Flags => flags;
    public TextLocation Location => location;
    public Symbol? Parent => parent;

    public string FullName
    {
        get
        {
            var parentName = Parent?.FullName;
            return string.IsNullOrEmpty(parentName) ? Name : $"{parentName}.{Name}";
        }
    }


    public (Symbol, bool existing) DeclareClass(string name, TextLocation location) => 
        DeclareSymbol(name, SymbolFlags.Class, location);
    
    public (Symbol, bool existing) DeclareField(string name, TextLocation location) => 
        DeclareSymbol(name, SymbolFlags.Field, location);
    
    public (Symbol, bool existing) DeclareMethod(string name, TextLocation location) => 
        DeclareSymbol(name, SymbolFlags.Method, location);

    public (Symbol, bool existing) DeclareSymbol(string name, SymbolFlags flags, TextLocation location)
    {
        _symbols ??= new();
        _symbolList ??= new();
        var symbol = new Symbol(name, flags, location, this);
        var existing = !_symbols.TryAdd(name, symbol);
        
        if(!existing) _symbolList.Add(symbol);
        
        return (existing ? _symbols[name] : symbol, existing);
    }

    public Symbol? Lookup(string name, bool includeParents = true) => 
        _symbols?.GetValueOrDefault(name) ?? this.Parent?.Lookup(name, includeParents);

    public IEnumerator<Symbol> GetEnumerator()
    {
        if(_symbolList == null)
            yield break;

        foreach (var symbol in _symbolList)
            yield return symbol;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}