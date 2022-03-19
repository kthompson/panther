using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding;

internal class Conversion
{
    public static readonly Conversion None = new Conversion(exists: false, isIdentity: false, isImplicit: false);
    public static readonly Conversion Identity = new Conversion(exists: true, isIdentity: true, isImplicit: true);
    public static readonly Conversion Implicit = new Conversion(exists: true, isIdentity: false, isImplicit: true);
    public static readonly Conversion Explicit = new Conversion(exists: true, isIdentity: false, isImplicit: false);

    public bool Exists { get; }
    public bool IsIdentity { get; }
    public bool IsImplicit { get; }
    public bool IsExplicit => Exists && !IsImplicit;

    private Conversion(bool exists, bool isIdentity, bool isImplicit)
    {
        Exists = exists;
        IsIdentity = isIdentity;
        IsImplicit = isImplicit;
    }

    public static Conversion Classify(Type from, Type to)
    {
        if (from == to)
            return Identity;

        if (to == Type.Any)
            return Implicit;

        if (from == Type.Any)
            return Explicit;

        if ((from == Type.Bool || from == Type.Int) && to == Type.String)
            return Explicit;

        if (from == Type.String && (to == Type.Bool || to == Type.Int))
            return Explicit;

        return None;
    }
}