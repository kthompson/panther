using System.Collections.Generic;

namespace Panther.CodeAnalysis.Symbols
{
    public static class SymbolExtensions
    {
        public static T WithFlags<T>(this T symbol, SymbolFlags flags) where T : Symbol
        {
            symbol.Flags |= flags;
            return symbol;
        }

        public static T WithType<T>(this T symbol, Type type) where T : Symbol
        {
            symbol.Type = type;
            return symbol;
        }
    }
}