using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class NamespaceOrTypeSymbol : Symbol
    {
        private readonly Dictionary<string, ImmutableArray<Symbol>> _symbols = new Dictionary<string, ImmutableArray<Symbol>>();

        public virtual ImmutableArray<Symbol> GetMembers() =>
            (from symbolList in _symbols.Values
                from symbol in symbolList
                select symbol).ToImmutableArray();

        public virtual ImmutableArray<Symbol> GetMembers(string name) =>
            _symbols.TryGetValue(name, out var symbols)
                ? symbols
                : ImmutableArray<Symbol>.Empty;

        public ImmutableArray<TypeSymbol> GetTypeMembers() =>
            GetMembers().OfType<TypeSymbol>().ToImmutableArray();
        public ImmutableArray<TypeSymbol> GetTypeMembers(string name) =>
            GetMembers(name).OfType<TypeSymbol>().ToImmutableArray();

        public bool DefineSymbol(Symbol symbol)
        {
            // only one field symbol
            if (symbol is FieldSymbol)
            {
                if (_symbols.ContainsKey(symbol.Name))
                    return false;

                _symbols.Add(symbol.Name, ImmutableArray.Create<Symbol>(symbol));
                return true;
            }

            if (_symbols.TryGetValue(symbol.Name, out var symbols))
            {
                _symbols[symbol.Name] = symbols.Add(symbol);
                return true;
            }

            _symbols.Add(symbol.Name, ImmutableArray.Create(symbol));
            return true;
        }

        public abstract bool IsType { get; }
        public bool IsNamespace => !IsType;

        protected NamespaceOrTypeSymbol(string name)
            : base(name)
        {
        }
    }
}