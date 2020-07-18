using System.Collections.Immutable;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Symbols
{
    public class SourceMethodSymbol : MethodSymbol
    {
        public FunctionDeclarationSyntax Declaration { get; }

        public SourceMethodSymbol(string name, ImmutableArray<ParameterSymbol> parameters, TypeSymbol returnType, FunctionDeclarationSyntax declaration)
            : base(name, parameters, returnType)
        {
            Declaration = declaration;
        }
    }
}