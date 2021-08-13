using System;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    public class ImportedMethodSymbol : MethodSymbol
    {
        public ImportedMethodSymbol(string name, ImmutableArray<ParameterSymbol> parameters, Type returnType)
            : base(Symbol.None, name, parameters, returnType)
        {
        }
    }
}