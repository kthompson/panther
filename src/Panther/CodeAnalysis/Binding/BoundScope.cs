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
        private readonly Dictionary<string, ImmutableArray<Symbol>> _symbols = new Dictionary<string, ImmutableArray<Symbol>>();
        private readonly Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _breakContinueLabels = new Stack<(BoundLabel, BoundLabel)>();
        private int _labelCounter = 0;

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

        public VariableSymbol? TryLookupVariable(string name)
        {
            if (_symbols.TryGetValue(name, out var existingSymbols))
            {
                return existingSymbols.OfType<VariableSymbol>().FirstOrDefault();
            }

            return Parent?.TryLookupVariable(name);
        }

        public TypeSymbol? TryLookupType(string name)
        {
            if (_symbols.TryGetValue(name, out var existingSymbols))
            {
                return existingSymbols.OfType<TypeSymbol>().FirstOrDefault();
            }

            return Parent?.TryLookupType(name);
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

        public void DeclareLoop(out BoundLabel breakLabel, out BoundLabel continueLabel)
        {
            var labels = GetLabelsStack();
            _labelCounter++;
            breakLabel = new BoundLabel($"break{_labelCounter}");
            continueLabel = new BoundLabel($"continue{_labelCounter}");

            labels.Push((breakLabel, continueLabel));
        }

        private Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> GetLabelsStack()
        {
            var root = this;
            while (root.Parent != null)
            {
                root = root.Parent;
            }

            return root._breakContinueLabels;
        }

        public BoundLabel? GetBreakLabel()
        {
            var labels = GetLabelsStack();
            return labels.Count == 0 ? null : labels.Peek().BreakLabel;
        }

        public BoundLabel? GetContinueLabel()
        {
            var labels = GetLabelsStack();
            return labels.Count == 0 ? null : labels.Peek().ContinueLabel;
        }

        public BoundScope EnterNamespace(NamespaceSymbol namespaceSymbol)
        {
            throw new System.NotImplementedException();
        }
    }
}