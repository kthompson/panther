using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal record BoundConversionExpression(SyntaxNode Syntax, TypeSymbol Type, BoundExpression Expression) : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.ConversionExpression;
    }
}