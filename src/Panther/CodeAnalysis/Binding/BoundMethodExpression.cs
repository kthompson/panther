using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    sealed record BoundMethodExpression : BoundNode
    {
        public BoundMethodExpression(SyntaxNode syntax, string name, ImmutableArray<MethodSymbol> methods)
            : base(syntax)
        {
            Name = name;
            Methods = methods;
        }

        public string Name { get; }
        public ImmutableArray<MethodSymbol> Methods { get; }

        public override BoundNodeKind Kind => BoundNodeKind.MethodExpression;
    }
}