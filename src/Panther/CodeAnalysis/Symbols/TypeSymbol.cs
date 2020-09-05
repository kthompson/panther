using System.Collections.Immutable;
using Panther.CodeAnalysis.Binding;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class TypeSymbol : NamespaceOrTypeSymbol
    {
        public override SymbolKind Kind => SymbolKind.Type;

        public TypeSymbol(string name) : base(name)
        {
        }

        public override string ToString() => this.Name;


        public static readonly TypeSymbol Error = new BoundType("err");

        // HACK: these should probably be ImportedTypeSymbols and imported into the BoundScope
        // but right now we reference them every where. Alternatively they could be something else
        // like a BuiltinType or something. that would also cover the `err` type
        public static readonly TypeSymbol Any = new BoundType("any");
        public static readonly TypeSymbol Unit = new BoundType("unit");

        public static readonly TypeSymbol Bool = new BoundType("bool");
        public static readonly TypeSymbol Int = new BoundType("int");
        public static readonly TypeSymbol String = new BoundType("string");

        public override bool IsType => true;
    }
}