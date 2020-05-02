using System.Collections.Generic;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class BuiltinFunctions
    {
        public static readonly MethodSymbol Println = new MethodSymbol("println",
            ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.Any, 0)), TypeSymbol.Unit);

        public static readonly MethodSymbol Print = new MethodSymbol("print",
            ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.Any, 0)), TypeSymbol.Unit);

        public static readonly MethodSymbol Read =
            new MethodSymbol("readLine", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);

        public static readonly MethodSymbol Rnd = new MethodSymbol("rnd",
            ImmutableArray.Create(new ParameterSymbol("max", TypeSymbol.Int, 0)), TypeSymbol.Int);


        // TODO: find a way to detect these and add them via the `Predef` namespace programatically
        public static readonly MethodSymbol GetOutput = new MethodSymbol("getOutput", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);
        public static readonly MethodSymbol MockReadLine = new MethodSymbol("mockReadLine", ImmutableArray.Create(new ParameterSymbol("value", TypeSymbol.String, 0)), TypeSymbol.Unit);


        public static IEnumerable<MethodSymbol> GetAll()
        {
            yield return Println;
            yield return Print;
            yield return Read;
            yield return Rnd;
            yield return GetOutput;
            yield return MockReadLine;
        }
    }
}