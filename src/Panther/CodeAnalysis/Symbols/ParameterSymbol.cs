using System;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    [Obsolete]
    public sealed class ParameterSymbol : LocalVariableSymbol
    {

        public ParameterSymbol(string name, Type type, int index)
            : base(name, isReadOnly: true, type: type, null)
        {
            this.Index = index;
            this.Flags |= SymbolFlags.Parameter;
        }
    }
}