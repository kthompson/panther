using System;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols;

public abstract record Type
{
    public virtual Symbol Symbol { get; }

    protected Type(Symbol symbol)
    {
        Symbol = symbol;
    }

    public static readonly Type Error = new TypeConstructor("err", TypeSymbol.Error);

    public static readonly Type Any = new TypeConstructor("any", TypeSymbol.Any);
    public static readonly Type Unit = new TypeConstructor("unit", TypeSymbol.Unit);

    public static readonly Type Bool = new TypeConstructor("bool", TypeSymbol.Bool);
    public static readonly Type Int = new TypeConstructor("int", TypeSymbol.Int);
    public static readonly Type String = new TypeConstructor("string", TypeSymbol.String);
    public static readonly Type Char = new TypeConstructor("char", TypeSymbol.Char);

    public static readonly Type Unresolved = new Unresolved();
    public static readonly Type NoType = new NoType();

    public static Type Delayed(Func<Type> f) => new DelayType(f);
}

public sealed record MethodType(ImmutableArray<Symbol> Parameters, Type ResultType)
    : Type(Symbol.None);

public sealed record IndexType(Type Left, Type Index) : Type(Symbol.None);

public sealed record ErrorType() : Type(TypeSymbol.Error);

public sealed record Unresolved() : Type(Symbol.None);

public sealed record NoType() : Type(Symbol.None);

public sealed record ClassType(Symbol Symbol) : Type(Symbol);

public sealed record NamespaceType(Symbol Symbol) : Type(Symbol);

public sealed record DelayType(Func<Type> F) : Type(Symbol.None)
{
    public override Symbol Symbol => F().Symbol;
}

public sealed record TypeConstructor(string Name, Symbol Symbol) : Type(Symbol)
{
    public override string ToString() => Name;
}

public static class TypeResolver
{
    public static Type Resolve(Type type) =>
        type switch
        {
            DelayType delayType => Resolve(delayType.F()),
            IndexType indexType => new IndexType(Resolve(indexType.Left), Resolve(indexType.Index)),
            MethodType methodType
                => new MethodType(methodType.Parameters, Resolve(methodType.ResultType)),
            ClassType
            or ErrorType
            or NamespaceType
            or NoType
            or TypeConstructor
            or Unresolved
                => type,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
}
