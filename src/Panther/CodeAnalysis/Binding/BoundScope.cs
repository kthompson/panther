using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundScope
    {
        private readonly MethodSymbol? _function;
        private readonly Dictionary<string, Symbol> _symbols = new Dictionary<string, Symbol>();
        private readonly Stack<(BoundLabel BreakLabel, BoundLabel ContinueLabel)> _breakContinueLabels = new Stack<(BoundLabel, BoundLabel)>();
        private int _labelCounter = 0;

        public BoundScope? Parent { get; }

        public BoundScope(BoundScope? parent)
            : this(parent, null) // should we pass parent._function here?
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

        public bool TryDeclareVariable(VariableSymbol variable) =>
            TryDeclare(variable);

        public bool TryDeclareFunction(MethodSymbol method) =>
            TryDeclare(method);

        private bool TryDeclare(Symbol variable)
        {
            if (_symbols.ContainsKey(variable.Name))
                return false;

            _symbols.Add(variable.Name, variable);
            return true;
        }

        public VariableSymbol? TryLookupVariable(string name)
        {
            if (_symbols.TryGetValue(name, out var existingSymbol))
            {
                if (existingSymbol is VariableSymbol outSymbol)
                {
                    return outSymbol;
                }

                return null;
            }

            return Parent?.TryLookupVariable(name);
        }

        public bool TryLookup(string name, out Symbol symbol)
        {
            if (_symbols.TryGetValue(name, out symbol))
                return true;

            return Parent != null && Parent.TryLookup(name, out symbol);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables() => _symbols.Values.OfType<VariableSymbol>().ToImmutableArray();

        public ImmutableArray<MethodSymbol> GetDeclaredFunctions() => _symbols.Values.OfType<MethodSymbol>().ToImmutableArray();

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
    }
}