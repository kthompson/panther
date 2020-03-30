namespace Panther.CodeAnalysis.Symbols
{
    public class TypeSymbol : Symbol
    {
        public static readonly TypeSymbol Error = new TypeSymbol("err");
        public static readonly TypeSymbol Unit = new TypeSymbol("unit");
        
        public static readonly TypeSymbol Bool = new TypeSymbol("bool");
        public static readonly TypeSymbol Int = new TypeSymbol("int");
        public static readonly TypeSymbol String = new TypeSymbol("string");
        
        public override SymbolKind Kind => SymbolKind.Type;

        private TypeSymbol(string name) : base(name)
        {
        }

        public override string ToString() => this.Name;
    }
}