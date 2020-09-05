using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundScope
    {
        private readonly MethodSymbol? _function;
        private readonly Dictionary<string, ImmutableArray<Symbol>> _importedSymbols = new Dictionary<string, ImmutableArray<Symbol>>();
        private readonly Dictionary<string, ImmutableArray<Symbol>> _symbols = new Dictionary<string, ImmutableArray<Symbol>>();

        public BoundScope? Parent { get; }

        public BoundScope(BoundScope? parent)
            : this(parent, parent?._function)
        {
        }

        public BoundScope(BoundScope? parent, MethodSymbol? function)
        {
            _function = function;
            Parent = parent;

            if (function == null)
                return;

            foreach (var parameter in function.Parameters)
            {
                TryDeclareVariable(parameter);
            }
        }

        public bool IsGlobalScope => _function == null;

        public bool TryDeclareVariable(VariableSymbol variable)
        {
            if (_symbols.ContainsKey(variable.Name))
                return false;

            _symbols.Add(variable.Name, ImmutableArray.Create<Symbol>(variable));
            return true;
        }

        public void Import(TypeSymbol symbol) => ImportSymbol(symbol);
        public void Import(MethodSymbol symbol) => ImportSymbol(symbol);

        public void ImportMembers(NamespaceOrTypeSymbol namespaceOrTypeSymbol)
        {
            foreach (var member in namespaceOrTypeSymbol.GetMembers())
            {
                ImportSymbol(member);
            }
        }

        private void ImportSymbol(Symbol symbol)
        {
            if (_symbols.TryGetValue(symbol.Name, out var symbols))
            {
                _symbols[symbol.Name] = symbols.Add(symbol);
                return;
            }

            _symbols.Add(symbol.Name, ImmutableArray.Create(symbol));
        }

        public VariableSymbol? LookupVariable(string name)
        {
            if (_symbols.TryGetValue(name, out var existingSymbols))
            {
                return existingSymbols.OfType<VariableSymbol>().FirstOrDefault();
            }

            return Parent?.LookupVariable(name);
        }

        public TypeSymbol? LookupType(string name)
        {
            if (_symbols.TryGetValue(name, out var existingSymbols))
            {
                return existingSymbols.OfType<TypeSymbol>().FirstOrDefault();
            }

            return Parent?.LookupType(name);
        }

        public ImmutableArray<MethodSymbol> LookupMethod(string name)
        {
            if (_symbols.TryGetValue(name, out var symbols))
            {
                return symbols.OfType<MethodSymbol>().ToImmutableArray();
            }

            return Parent?.LookupMethod(name) ?? ImmutableArray<MethodSymbol>.Empty;
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables() => _symbols.Values.SelectMany(symbols => symbols).OfType<VariableSymbol>().ToImmutableArray();

        public ImmutableArray<MethodSymbol> GetDeclaredFunctions() => _symbols.Values.SelectMany(symbols => symbols).OfType<MethodSymbol>().ToImmutableArray();

        public BoundScope EnterNamespace(NamespaceSymbol namespaceSymbol)
        {
            throw new System.NotImplementedException();
        }
    }
}