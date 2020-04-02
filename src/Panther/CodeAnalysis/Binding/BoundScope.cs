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
        private readonly Dictionary<string, ImmutableArray<FunctionSymbol>> _functions = new Dictionary<string, ImmutableArray<FunctionSymbol>>();

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

        public bool TryDeclareFunction(FunctionSymbol function)
        {
            if (_functions.TryGetValue(function.Name, out var functions))
            {
                // TODO check if any of the existing methods have identical signatures
                _functions[function.Name] = functions.Add(function);
                return true;
            }

            _functions.Add(function.Name, ImmutableArray.Create(function));
            return true;
        }

        public bool TryLookup(string name, out VariableSymbol variable)
        {
            if (_variables.TryGetValue(name, out variable))
                return true;

            return Parent != null && Parent.TryLookup(name, out variable);
        }

        public FunctionLookupResult TryLookupFunction(string name, ImmutableArray<TypeSymbol> argTypes)
        {
            if (_functions.TryGetValue(name, out var functions))
            {
                var function = functions.FirstOrDefault(f => f.Parameters.Select(x => x.Type).SequenceEqual(argTypes));

                if (function == null)
                {
                    return new FunctionLookupFailure(FunctionLookupFailureType.NoOverloads);
                }

                return new FunctionLookupSuccess(function);
            }

            if (Parent != null)
            {
                return Parent.TryLookupFunction(name, argTypes);
            }

            return new FunctionLookupFailure(FunctionLookupFailureType.Undefined);
        }

        public ImmutableArray<VariableSymbol> GetDeclaredVariables() => _variables.Values.ToImmutableArray();

        public abstract class FunctionLookupResult
        {
        }

        internal enum FunctionLookupFailureType
        {
            Undefined,
            NoOverloads,
        }

        public class FunctionLookupFailure : FunctionLookupResult
        {
            public FunctionLookupFailureType Message { get; }

            public FunctionLookupFailure(FunctionLookupFailureType message)
            {
                Message = message;
            }
        }

        public sealed class FunctionLookupSuccess : FunctionLookupResult
        {
            public FunctionSymbol Function { get; }

            public FunctionLookupSuccess(FunctionSymbol function)
            {
                Function = function;
            }
        }
    }
}