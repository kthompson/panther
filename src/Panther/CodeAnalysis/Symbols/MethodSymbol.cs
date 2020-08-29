using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Symbols
{
    public abstract class MethodSymbol : Symbol
    {
        public TypeSymbol DeclaringType { get; }
        public ImmutableArray<ParameterSymbol> Parameters { get; }
        public TypeSymbol ReturnType { get; }

        public override SymbolKind Kind => SymbolKind.Method;

        protected MethodSymbol(TypeSymbol declaringType, string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType)
            : base(name)
        {
            DeclaringType = declaringType;
            Parameters = parameters;
            ReturnType = returnType;
        }
    }
}