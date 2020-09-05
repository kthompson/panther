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
        private readonly NamespaceOrTypeSymbol? _containingSymbol;

        // symbols that have not been defined but are needed in scope for resolving types etc
        private readonly Dictionary<string, ImmutableArray<Symbol>> _importedSymbols = new Dictionary<string, ImmutableArray<Symbol>>();

        public BoundScope? Parent { get; }

        public BoundScope(BoundScope? parent)
            : this(parent, parent?._containingSymbol)
        {
        }

        public BoundScope(BoundScope? parent, NamespaceOrTypeSymbol? container)
        {
            _containingSymbol = container;
            Parent = parent;
        }

        public BoundScope(BoundScope? parent, MethodSymbol function)
        {
            _containingSymbol = null;
            Parent = parent;

            foreach (var parameter in function.Parameters)
            {
                DefineSymbol(parameter);
            }
        }

        public bool IsGlobalScope => _containingSymbol != null &&
                                     _containingSymbol.Name == "$Program" &&
                                     _containingSymbol.IsType;

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
            if (_containingSymbol != null)
                return _containingSymbol.DefineSymbol(symbol);

            ImportSymbol(symbol);
            return true;
        }

        public VariableSymbol? LookupVariable(string name)
        {
            var variable = _containingSymbol?.GetMembers(name).OfType<VariableSymbol>().FirstOrDefault();
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
            var type = _containingSymbol?.GetTypeMembers(name).FirstOrDefault();
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
            var methods = _containingSymbol?.GetMembers(name).OfType<MethodSymbol>().ToImmutableArray();

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