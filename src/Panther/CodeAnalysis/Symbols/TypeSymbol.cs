using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class TypeSymbol : Symbol
    {
        public ImmutableArray<MethodSymbol> Methods { get; }
        public ImmutableArray<FieldSymbol> Fields { get; }

        public override SymbolKind Kind => SymbolKind.Type;

        public TypeSymbol(string name, ImmutableArray<MethodSymbol> methods, ImmutableArray<FieldSymbol> fields) : base(name)
        {
            Methods = methods;
            Fields = fields;
        }

        public override string ToString() => this.Name;


        public static readonly TypeSymbol Error = new TypeSymbol("err", ImmutableArray<MethodSymbol>.Empty, ImmutableArray<FieldSymbol>.Empty);
        public static readonly TypeSymbol Any = new TypeSymbol("any", ImmutableArray<MethodSymbol>.Empty, ImmutableArray<FieldSymbol>.Empty);
        public static readonly TypeSymbol Unit = new TypeSymbol("unit", ImmutableArray<MethodSymbol>.Empty, ImmutableArray<FieldSymbol>.Empty);

        public static readonly TypeSymbol Bool = new TypeSymbol("bool", ImmutableArray<MethodSymbol>.Empty, ImmutableArray<FieldSymbol>.Empty);
        public static readonly TypeSymbol Int = new TypeSymbol("int", ImmutableArray<MethodSymbol>.Empty, ImmutableArray<FieldSymbol>.Empty);
        public static readonly TypeSymbol String = new TypeSymbol("string", ImmutableArray<MethodSymbol>.Empty, ImmutableArray<FieldSymbol>.Empty);
    }
}