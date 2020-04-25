using System.Collections.Generic;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class BuiltinFunctions
    {
        public static readonly FunctionSymbol Print = new FunctionSymbol("print",
            ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String, 0)), TypeSymbol.Unit);

        public static readonly FunctionSymbol Read =
            new FunctionSymbol("read", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);

        public static readonly FunctionSymbol Rnd = new FunctionSymbol("rnd",
            ImmutableArray.Create(new ParameterSymbol("max", TypeSymbol.Int, 0)), TypeSymbol.Int);

        public static IEnumerable<FunctionSymbol> GetAll()
        {
            yield return Print;
            yield return Read;
            yield return Rnd;
        }
    }
}