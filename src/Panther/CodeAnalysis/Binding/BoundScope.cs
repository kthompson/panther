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

        public BoundScope Parent { get; }

        public BoundScope(BoundScope parent)
            : this(parent, parent.Symbol)
        {
        }

        public BoundScope(Symbol symbol)
        {
            this.Symbol = symbol;
            this.Parent = this;
        }

        public BoundScope(BoundScope parent, Symbol container)
        {
            Symbol = container;
            Parent = parent;
        }

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

        public Symbol? LookupVariable(string name)
        {
            var variable = Symbol.LookupMembers(name).FirstOrDefault(v => v.IsValue);
            if (variable != null)
                return variable;

            if (_importedSymbols.TryGetValue(name, out var importedSymbols))
            {
                return importedSymbols.FirstOrDefault();
            }

            if (IsRootScope)
                return null;

            return Parent.LookupVariable(name);
        }

        public Symbol? LookupType(string name)
        {
            var type = Symbol.LookupType(name);
            if (type != null)
                return type;

            if (_importedSymbols.TryGetValue(name, out var importedSymbols))
            {
                return importedSymbols.FirstOrDefault(t => t.IsType);
            }

            if (IsRootScope)
                return null;

            return Parent.LookupType(name);
        }

        public ImmutableArray<Symbol> LookupSymbol(string name, bool deep = true)
        {
            var members = Symbol.LookupMembers(name).ToImmutableArray();
            if (members.Any())
                return members;

            if (_importedSymbols.TryGetValue(name, out var symbols))
            {
                return symbols.ToImmutableArray();
            }

            if (deep)
            {
                if (IsRootScope)
                    return ImmutableArray<Symbol>.Empty;

                return Parent.LookupSymbol(name);
            }

            return ImmutableArray<Symbol>.Empty;
        }

        public ImmutableArray<Symbol> LookupMethod(string name, bool deep = true)
        {
            var methods = Symbol.LookupMembers(name).Where(m => m.IsMethod).ToImmutableArray();

            if (methods.Any())
                return methods;

            if (_importedSymbols.TryGetValue(name, out var symbols))
            {
                return symbols.Where(m => m.IsMethod).ToImmutableArray();
            }

            if (deep)
            {
                if (IsRootScope)
                    return ImmutableArray<Symbol>.Empty;

                return Parent.LookupMethod(name);
            }

            return ImmutableArray<Symbol>.Empty;
        }

        public ImmutableArray<Symbol> GetDeclaredVariables()
        {
            // TODO. do we even need this if we have the container type?
            // return _ownedSymbols.Values.SelectMany(symbols => symbols).OfType<VariableSymbol>().ToImmutableArray();
            return ImmutableArray<Symbol>.Empty;
        }

        public BoundScope EnterNamespace(Symbol namespaceSymbol)
        {
            throw new System.NotImplementedException();
        }
    }
}