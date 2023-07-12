using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Typing;

internal abstract record Conversion
{
    public static readonly Conversion None = new NoConversion();
    public static readonly Conversion Identity = new IdentityConversion();
    public static readonly Conversion Implicit = new ImplicitConversion();
    public static readonly Conversion Explicit = new ExplicitConversion();

    sealed record NoConversion : Conversion;

    sealed record IdentityConversion : Conversion;

    sealed record ExplicitConversion : Conversion;

    sealed record ImplicitConversion : Conversion;

    protected Conversion() { }

    public bool Exists => this is not NoConversion;
    public bool IsImplicit => this is ImplicitConversion;
    public bool IsExplicit => this is ExplicitConversion;
    public bool IsIdentity => this is IdentityConversion;

    public static Conversion Classify(Type from, Type to)
    {
        if (from == to)
            return Identity;

        if (to == Type.Any)
            return Implicit;

        if (from == Type.Any)
            return Explicit;

        if (from == Type.Int && to == Type.Char)
            return Explicit;

        if (from == Type.Null && !IsValueType(to))
            return Implicit;

        if ((from == Type.Bool || from == Type.Int || from == Type.Char) && to == Type.String)
            return Explicit;

        if (from == Type.String && (to == Type.Bool || to == Type.Int))
            return Explicit;

        if (
            from is ArrayType(_, var fromElement)
            && to is ArrayType(_, var toElement)
            && fromElement == toElement
        )
            return Identity;

        return None;
    }

    private static bool IsValueType(Type type) => type == Type.Bool || type == Type.Int || type == Type.Char;
}
