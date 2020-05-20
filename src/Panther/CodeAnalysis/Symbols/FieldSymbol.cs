namespace Panther.CodeAnalysis.Symbols
{
    public sealed class FieldSymbol : Symbol
    {
        public TypeSymbol Type { get; }

        public FieldSymbol(string name, TypeSymbol type) : base(name)
        {
            Type = type;
        }

        public override SymbolKind Kind => SymbolKind.Field;
    }
}