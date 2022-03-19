using System.Collections.Immutable;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Symbols;

public abstract class TypeSymbol : Symbol
{
    public TypeSymbol(Symbol owner, TextLocation location, string name) : base(owner, location, name)
    {
    }

    public override string ToString() => this.Name;


    public static readonly TypeSymbol Error = new BoundType(Symbol.None, TextLocation.None, "err");

    // HACK: these should probably be AliasSymbols that reference the real System.* imported types
    // but right now we reference them every where. Alternatively they could be something else
    // like a BuiltinType or something. that would also cover the `err` type
    public static readonly TypeSymbol Any = new BoundType(Symbol.None, TextLocation.None, "any")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.Any);

    public static readonly TypeSymbol Unit = new BoundType(Symbol.None, TextLocation.None, "unit")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.Unit);

    public static readonly TypeSymbol Bool = new BoundType(Symbol.None, TextLocation.None, "bool")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.Bool);

    public static readonly TypeSymbol Int = new BoundType(Symbol.None, TextLocation.None, "int")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.Int);

    public static readonly TypeSymbol String = new BoundType(Symbol.None, TextLocation.None, "string")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.String);
}