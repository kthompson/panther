using System.Collections.Immutable;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Symbols
{
    public class SourceMethodSymbol : MethodSymbol
    {
        public FunctionDeclarationSyntax Declaration { get; }

        public SourceMethodSymbol(TypeSymbol declaringType, string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType, FunctionDeclarationSyntax declaration)
            : base(declaringType, name, parameters, returnType)
        {
            Declaration = declaration;
        }
    }
}