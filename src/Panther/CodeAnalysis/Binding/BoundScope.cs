using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundScope
    {
        private readonly Dictionary<string, VariableSymbol> _variables = new Dictionary<string, VariableSymbol>();
        private readonly List<FunctionSymbol> _functions = new List<FunctionSymbol>();

        public BoundScope? Parent { get; }

        public BoundScope(BoundScope? parent)
        {
            Parent = parent;
        }

        public bool TryDeclare(VariableSymbol variable)
        {
            if (_variables.ContainsKey(variable.Name))
                return false;

            _variables.Add(variable.Name, variable);
            return true;
        }

        public bool TryLookup(string name, out VariableSymbol variable)
        {
            if (_variables.TryGetValue(name, out variable))
                return true;

            return Parent != null && Parent.TryLookup(name, out variable);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables() => _variables.Values.ToImmutableArray();
    }
}