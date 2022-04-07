using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols;

public abstract record Type(Symbol Symbol)
{
    public static readonly Type Error = new TypeConstructor("err", TypeSymbol.Error);

    public static readonly Type Any = new TypeConstructor("any", TypeSymbol.Any);
    public static readonly Type Unit = new TypeConstructor("unit", TypeSymbol.Unit);

    public static readonly Type Bool = new TypeConstructor("bool", TypeSymbol.Bool);
    public static readonly Type Int = new TypeConstructor("int", TypeSymbol.Int);
    public static readonly Type String = new TypeConstructor("string", TypeSymbol.String);

    public static readonly Type Unresolved = new Unresolved();
    public static readonly Type NoType = new NoType();
}

public sealed record MethodType(ImmutableArray<Symbol> Parameters, Type ResultType) : Type(Symbol.None);
public sealed record ErrorType() : Type(TypeSymbol.Error);
public sealed record Unresolved() : Type(Symbol.None);
public sealed record NoType() : Type(Symbol.None);
public sealed record ClassType(Symbol Symbol) : Type(Symbol);
public sealed record NamespaceType(Symbol Symbol) : Type(Symbol);

public sealed record TypeConstructor(string Name, Symbol Symbol) : Type(Symbol)
{
    public override string ToString() => Name;
};