using Panther.CodeAnalysis.Binding;

namespace Panther.CodeAnalysis.Symbols
{
    public class LocalVariableSymbol : VariableSymbol
    {
        internal LocalVariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constantValue)
            : base(name, isReadOnly, type, constantValue)
        {
        }
    }
}