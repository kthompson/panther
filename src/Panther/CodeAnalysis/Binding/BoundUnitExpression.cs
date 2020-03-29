using System;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundUnitExpression : BoundExpression
    {
        
        public static  readonly BoundUnitExpression Default = new BoundUnitExpression();

        private BoundUnitExpression()
        {
            
        }
        public override BoundNodeKind Kind => BoundNodeKind.UnitExpression;
        public override TypeSymbol Type => TypeSymbol.Unit;
    }
}