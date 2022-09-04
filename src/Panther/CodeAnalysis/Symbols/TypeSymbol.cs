using System;
using System.Collections.Immutable;
using System.IO;
using Panther.CodeAnalysis.Text;
using Panther.CodeAnalysis.Typing;

namespace Panther.CodeAnalysis.Symbols;

public abstract class TypeSymbol : Symbol
{
    public TypeSymbol(Symbol owner, TextLocation location, string name)
        : base(owner, location, name) { }

    public override string ToString() => this.Name;

    public static readonly TypeSymbol Error = new TypedType(Symbol.None, TextLocation.None, "err");

    // HACK: these should probably be AliasSymbols that reference the real System.* imported types
    // but right now we reference them every where. Alternatively they could be something else
    // like a BuiltinType or something. that would also cover the `err` type
    public static readonly TypeSymbol Any = new TypedType(Symbol.None, TextLocation.None, "any")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.Any);

    public static readonly TypeSymbol Bool = new TypedType(Symbol.None, TextLocation.None, "bool")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.Bool);

    public static readonly TypeSymbol Char = new TypedType(Symbol.None, TextLocation.None, "char")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.Char);

    public static readonly TypeSymbol Int = new TypedType(Symbol.None, TextLocation.None, "int")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.Int);

    public static readonly TypeSymbol String;

    public static readonly TypeSymbol Unit = new TypedType(Symbol.None, TextLocation.None, "unit")
        .WithFlags(SymbolFlags.Class)
        .WithType(Type.Unit);

    public static Symbol ArrayOf(Symbol symbol)
    {
        var type = new TypedType(Symbol.None, TextLocation.None, $"Array<{symbol.Name}>")
            .WithFlags(SymbolFlags.Class)
            .WithType(Type.Delayed(() => Type.ArrayOf(symbol)));

        type.NewTerm(TextLocation.None, "Length", SymbolFlags.Property)
            .WithType(Type.Int)
            .Declare();

        return type;
    }

    static TypeSymbol()
    {
        String = new TypedType(Symbol.None, TextLocation.None, "string")
            .WithFlags(SymbolFlags.Class)
            .WithType(Type.Delayed(() => Type.String));

        String
            .NewTerm(TextLocation.None, "Length", SymbolFlags.Property)
            .WithType(Type.Delayed(() => Type.Int))
            .Declare();

        var getItem = String.NewMethod(TextLocation.None, "get_Chars").Declare();
        getItem
            .NewParameter(TextLocation.None, "i", 0)
            .WithType(Type.Delayed(() => Type.Int))
            .Declare();

        getItem.Type = new MethodType(getItem.Parameters, Type.Delayed(() => Type.Char));
    }
}
