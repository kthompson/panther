using System;

namespace Panther.CodeAnalysis.Syntax
{
    public enum OperatorPrecedence : byte
    {
        Lowest = 0,

        Prefix = byte.MaxValue,
    }
}