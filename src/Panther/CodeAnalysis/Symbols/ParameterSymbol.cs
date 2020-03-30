namespace Panther.CodeAnalysis.Symbols
{
    public sealed class ParameterSymbol : VariableSymbol
    {
        public override SymbolKind Kind => SymbolKind.Parameter;

        public ParameterSymbol(string name, TypeSymbol type) 
            : base(name, isReadOnly: true, type)
        {
        }
    }
}