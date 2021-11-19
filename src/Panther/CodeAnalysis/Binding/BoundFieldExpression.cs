using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    sealed record BoundFieldExpression(SyntaxNode Syntax, string Name, BoundExpression? Expression, Symbol Field) : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.FieldExpression;
        public override Type Type { get ; init; } = Field.Type;
        public override void Accept(BoundNodeVisitor visitor) => visitor.VisitFieldExpression(this);

        public override TResult Accept<TResult>(BoundNodeVisitor<TResult> visitor) => visitor.VisitFieldExpression(this);

    }
}