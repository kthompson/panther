using System.Collections.Immutable;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    public class ImportedMethodSymbol : MethodSymbol
    {
        public ImportedMethodSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType)
            : base(name, parameters, returnType)
        {
        }
    }
}