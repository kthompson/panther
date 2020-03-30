using System.Collections.Generic;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : TypeSymbol
    {
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }

        public override SymbolKind Kind => SymbolKind.Function;

        public FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType)
            : base(name)
        {
            Parameters = parameters;
            ReturnType = returnType;
        }
    }

    public sealed class BuiltinFunctions
    {
        public static readonly FunctionSymbol Print = new FunctionSymbol("print", ImmutableArray.Create(new ParameterSymbol("text", TypeSymbol.String)), TypeSymbol.Unit);
        public static readonly FunctionSymbol Read = new FunctionSymbol("read", ImmutableArray<ParameterSymbol>.Empty, TypeSymbol.String);

        public static IEnumerable<FunctionSymbol> GetAll()
        {
            yield return Print;
            yield return Read;
        }
    }
}