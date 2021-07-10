using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundScope
    {
        public Symbol Symbol { get; }

        // symbols that have not been defined but are needed in scope for resolving types etc
        private readonly Dictionary<string, ImmutableArray<Symbol>> _importedSymbols = new Dictionary<string, ImmutableArray<Symbol>>();

        public BoundScope? Parent { get; }

        public BoundScope(BoundScope parent)
            : this(parent, parent.Symbol)
        {
        }

        public BoundScope(Symbol symbol)
        {
            this.Symbol = symbol;
        }

        public BoundScope(BoundScope? parent, Symbol container)
        {
            Symbol = container;
            Parent = parent;
        }

        public bool IsGlobalScope => Symbol.IsType && Symbol.Name == "$Program";

        public void Import(TypeSymbol symbol) => ImportSymbol(symbol);

        public void ImportMembers(NamespaceOrTypeSymbol namespaceOrTypeSymbol)
        {
            foreach (var member in namespaceOrTypeSymbol.GetMembers())
            {
                ImportSymbol(member);
            }
        }

        private void ImportSymbol(Symbol symbol)
        {
            if (_importedSymbols.TryGetValue(symbol.Name, out var symbols))
            {
                _importedSymbols[symbol.Name] = symbols.Add(symbol);
                return;
            }

            _importedSymbols.Add(symbol.Name, ImmutableArray.Create(symbol));
        }

        public bool DefineSymbol(Symbol symbol)
        {
            return Symbol.DefineSymbol(symbol);
        }

        public VariableSymbol? LookupVariable(string name)
        {
            var variable = Symbol?.GetMembers(name).OfType<VariableSymbol>().FirstOrDefault();
            if (variable != null)
                return variable;

            if (_importedSymbols.TryGetValue(name, out var importedSymbols))
            {
                return importedSymbols.OfType<VariableSymbol>().FirstOrDefault();
            }

            return Parent?.LookupVariable(name);
        }

        public TypeSymbol? LookupType(string name)
        {
            var type = Symbol?.GetTypeMembers(name).FirstOrDefault();
            if (type != null)
                return type;

            if (_importedSymbols.TryGetValue(name, out var importedSymbols))
            {
                return importedSymbols.OfType<TypeSymbol>().FirstOrDefault();
            }

            return Parent?.LookupType(name);
        }

        public ImmutableArray<MethodSymbol> LookupMethod(string name)
        {
            var methods = Symbol?.GetMembers(name).OfType<MethodSymbol>().ToImmutableArray();

            if (methods != null && methods.Value.Any())
                return methods.Value;

            if (_importedSymbols.TryGetValue(name, out var symbols))
            {
                return symbols.OfType<MethodSymbol>().ToImmutableArray();
            }

            return Parent?.LookupMethod(name) ?? ImmutableArray<MethodSymbol>.Empty;
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables()
        {
            // TODO. do we even need this if we have the container type?
            // return _ownedSymbols.Values.SelectMany(symbols => symbols).OfType<VariableSymbol>().ToImmutableArray();
            return ImmutableArray<VariableSymbol>.Empty;
        }

        public ImmutableArray<MethodSymbol> GetDeclaredMethods()
        {
            // TODO. do we even need this if we have the container type?
            // return _ownedSymbols.Values.SelectMany(symbols => symbols).OfType<MethodSymbol>().ToImmutableArray();
            return ImmutableArray<MethodSymbol>.Empty;
        }

        public BoundScope EnterNamespace(NamespaceSymbol namespaceSymbol)
        {
            throw new System.NotImplementedException();
        }
    }
}