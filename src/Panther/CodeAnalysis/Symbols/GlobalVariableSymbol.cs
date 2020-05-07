using Panther.CodeAnalysis.Binding;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class GlobalVariableSymbol : VariableSymbol
    {
        internal GlobalVariableSymbol(string name, bool isReadOnly, TypeSymbol type, BoundConstant? constantValue)
            : base(name, isReadOnly, type, constantValue)
        {
        }

        public override SymbolKind Kind => SymbolKind.GlobalVariable;
    }
}