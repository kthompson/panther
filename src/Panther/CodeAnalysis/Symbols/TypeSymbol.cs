using System.Collections.Immutable;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class TypeSymbol : NamespaceOrTypeSymbol
    {
        public TypeSymbol(Symbol owner, TextLocation location, string name) : base(owner, location, name)
        {
        }

        public override string ToString() => this.Name;


        public static readonly TypeSymbol Error = new BoundType(Symbol.None, TextLocation.None, "err");

        // HACK: these should probably be AliasSymbols that reference the real System.* imported types
        // but right now we reference them every where. Alternatively they could be something else
        // like a BuiltinType or something. that would also cover the `err` type
        public static readonly TypeSymbol Any = new BoundType(Symbol.None, TextLocation.None, "any");
        public static readonly TypeSymbol Unit = new BoundType(Symbol.None, TextLocation.None, "unit");

        public static readonly TypeSymbol Bool = new BoundType(Symbol.None, TextLocation.None, "bool");
        public static readonly TypeSymbol Int = new BoundType(Symbol.None, TextLocation.None, "int");
        public static readonly TypeSymbol String = new BoundType(Symbol.None, TextLocation.None, "string");

        public override bool IsType => true;
    }
}