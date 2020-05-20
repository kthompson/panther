using System.Collections.Generic;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class BuiltinFunctions
    {
        public static readonly MethodSymbol Print = new MethodSymbol("println",
            ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String, 0)), TypeSymbol.Unit);

        public static readonly MethodSymbol Read =
            new MethodSymbol("read", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);

        public static readonly MethodSymbol Rnd = new MethodSymbol("rnd",
            ImmutableArray.Create(new ParameterSymbol("max", TypeSymbol.Int, 0)), TypeSymbol.Int);

        public static IEnumerable<MethodSymbol> GetAll()
        {
            yield return Print;
            yield return Read;
            yield return Rnd;
        }
    }
}