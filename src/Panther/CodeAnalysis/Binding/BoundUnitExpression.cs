using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundUnitExpression : BoundExpression
    {
        public BoundUnitExpression(SyntaxNode syntax) : base(syntax)
        {
        }

        public override BoundNodeKind Kind => BoundNodeKind.UnitExpression;

        public override Type Type { get; init; } = Type.Unit;
    }
}