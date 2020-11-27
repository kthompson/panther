using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    sealed record BoundFieldExpression(SyntaxNode Syntax, string Name, FieldSymbol Field) : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.FieldExpression;
        public override TypeSymbol Type { get ; init; } = Field.Type;
    }
}