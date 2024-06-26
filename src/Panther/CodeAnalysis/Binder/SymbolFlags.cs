using System;

namespace Panther.CodeAnalysis.Binder;

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

    // Symbol Access
    Private = None, // may not use this but should eventually be the default access level
    Protected = 1 << 8,
    Public = 1 << 9,
}
