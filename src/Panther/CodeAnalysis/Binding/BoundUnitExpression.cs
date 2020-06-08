using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundUnitExpression : BoundExpression
    {
        public BoundUnitExpression(SyntaxNode syntax) : base(syntax)
        {
        }

        public override BoundNodeKind Kind => BoundNodeKind.UnitExpression;
        public override TypeSymbol Type => TypeSymbol.Unit;
    }
}