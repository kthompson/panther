using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class MethodSymbol : Symbol
    {
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }

        protected MethodSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType)
            : base(Symbol.None, TextLocation.None, name)
        {
            Parameters = parameters;
            ReturnType = returnType;
            foreach (var parameter in parameters)
            {
                this.DefineSymbol(parameter);
            }
        }
    }
}