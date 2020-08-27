using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class TypeSymbol : TypeOrNamespaceSymbol
    {
        public string Namespace { get; }

        public override SymbolKind Kind => SymbolKind.Type;

        public TypeSymbol(string ns, string name) : base(name)
        {
            Namespace = ns;
        }

        public override string ToString() => this.Name;


        public static readonly TypeSymbol Error = new TypeSymbol("", "err");
        public static readonly TypeSymbol Any = new TypeSymbol("", "any");
        public static readonly TypeSymbol Unit = new TypeSymbol("", "unit");

        public static readonly TypeSymbol Bool = new TypeSymbol("", "bool");
        public static readonly TypeSymbol Int = new TypeSymbol("", "int");
        public static readonly TypeSymbol String = new TypeSymbol("", "string");
        public override ImmutableArray<Symbol> GetMembers()
        {
            throw new System.NotImplementedException();
        }

        public override ImmutableArray<Symbol> GetMembers(string name)
        {
            throw new System.NotImplementedException();
        }

        public override ImmutableArray<TypeSymbol> GetTypeMembers()
        {
            throw new System.NotImplementedException();
        }

        public override ImmutableArray<TypeSymbol> GetTypeMembers(string name)
        {
            throw new System.NotImplementedException();
        }

        public override bool IsType => true;
    }
}