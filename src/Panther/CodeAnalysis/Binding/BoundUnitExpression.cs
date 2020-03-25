using System;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundUnitExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.UnitExpression;
        public override Type Type => typeof(Unit);
    }
}