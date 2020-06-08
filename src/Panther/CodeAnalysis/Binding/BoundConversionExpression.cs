using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundConversionExpression : BoundExpression
    {
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.ConversionExpression;
        public override TypeSymbol Type { get; }

        public BoundConversionExpression(SyntaxNode syntax, TypeSymbol type, BoundExpression expression)
            : base(syntax)
        {
            Type = type;
            Expression = expression;
        }
    }
}