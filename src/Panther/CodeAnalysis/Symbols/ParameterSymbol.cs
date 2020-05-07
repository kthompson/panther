namespace Panther.CodeAnalysis.Symbols
{
    public sealed class ParameterSymbol : LocalVariableSymbol
    {
        public int Index { get; }
        public override SymbolKind Kind => SymbolKind.Parameter;

        public ParameterSymbol(string name, TypeSymbol type, int index)
            : base(name, isReadOnly: true, type: type, null)
        {
            Index = index;
        }
    }
}