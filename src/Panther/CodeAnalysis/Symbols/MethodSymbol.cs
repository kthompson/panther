using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class MethodSymbol : Symbol
    {

        protected MethodSymbol(Symbol owner, string name, ImmutableArray<ParameterSymbol> parameters, Type returnType)
            : base(owner, TextLocation.None, name)
        {
            this.Type = new MethodType(parameters.OfType<Symbol>().ToImmutableArray(), returnType);
            this.Flags |= SymbolFlags.Method;

            foreach (var parameter in parameters)
            {
                this.DefineSymbol(parameter);
            }
        }
    }
}