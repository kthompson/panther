using System;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundUnitExpression : BoundExpression
    {
        public override BoundNodeKind Kind => BoundNodeKind.UnitExpression;
        public override TypeSymbol Type => TypeSymbol.Unit;
    }
}