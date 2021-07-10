using Panther.CodeAnalysis.Binding;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class FieldSymbol : VariableSymbol
    {
        internal FieldSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constantValue)
            : base(name, isReadOnly, type, constantValue)
        {
        }
    }
}