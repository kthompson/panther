using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    sealed record BoundFieldExpression(SyntaxNode Syntax, string Name, Symbol Field) : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.FieldExpression;
        public override Type Type { get ; init; } = Field.Type;
    }
}