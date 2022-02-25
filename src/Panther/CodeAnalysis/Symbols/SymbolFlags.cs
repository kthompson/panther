using System;

namespace Panther.CodeAnalysis.Symbols
{
    [Flags]
    public enum SymbolFlags
    {
        None = 0,

        // Symbol type
        Namespace = 1,
        Object = 1 << 1,
        Class = 1 << 2,
        Method = 1 << 3,
        Field = 1 << 4,
        Property = 1 << 5,
        Parameter = 1 << 6,
        Local = 1 << 7,

        // Access
        Static = 1 << 8, // methods and fields
        Readonly = 1 << 9,

        Value = Namespace | Object | Class | Method | Field | Property | Parameter | Local,

        Member = Object | Class,

        /// <summary>
        /// Namespaces can merge with other namespaces
        /// </summary>
        NamespaceExcludes = Value & ~Namespace,

        // Classes can coexist with Objects
        ClassExcludes = Value & ~Object,

        // Objects can coexist with Classes
        ObjectExcludes = Value & ~Class,

        // Methods can collide with other methods but
        // ONLY if their signatures are different
        MethodExcludes = Value & ~Method,

        ParameterExcludes = Value,
        LocalExcludes = Value,
    }
}
