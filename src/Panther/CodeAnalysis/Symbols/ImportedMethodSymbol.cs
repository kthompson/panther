using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public class ImportedMethodSymbol : MethodSymbol
    {
        public ImportedMethodSymbol(TypeSymbol declaringType, string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType)
            : base(name, parameters, returnType)
        {
        }
    }
}