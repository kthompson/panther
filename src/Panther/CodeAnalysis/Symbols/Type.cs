using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract record Type()
    {
        public static readonly Type Error = new TypeConstructor("err");

        public static readonly Type Any = new TypeConstructor("any");
        public static readonly Type Unit = new TypeConstructor("unit");

        public static readonly Type Bool = new TypeConstructor("bool");
        public static readonly Type Int = new TypeConstructor("int");
        public static readonly Type String = new TypeConstructor("string");

        public static readonly Type NoType = new NoType();
    }

    public sealed record MethodType(ImmutableArray<Symbol> Parameters, Type ResultType) : Type;
    public sealed record ErrorType() : Type;
    public sealed record NoType() : Type;
    public sealed record ClassType(ImmutableArray<Symbol> Declarations) : Type;
    public sealed record TypeConstructor(string Name) : Type;
}