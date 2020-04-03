using System.Collections.Immutable;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Symbols
{
    public sealed class FunctionSymbol : TypeSymbol
    {
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }
        public FunctionDeclarationSyntax Declaration { get; }

        public override SymbolKind Kind => SymbolKind.Function;

        public FunctionSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType, FunctionDeclarationSyntax declaration = null)
            : base(name)
        {
            Parameters = parameters;
            ReturnType = returnType;
            Declaration = declaration;
        }
    }
}