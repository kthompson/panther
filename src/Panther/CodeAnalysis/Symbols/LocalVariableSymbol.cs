using System;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    [Obsolete]
    public class LocalVariableSymbol : VariableSymbol
    {
        internal LocalVariableSymbol(string name, bool isReadOnly, Type type, BoundConstant? constantValue)
            : base(name, isReadOnly, type, constantValue)
        {
            this.Flags |= SymbolFlags.Local;
        }
    }
}