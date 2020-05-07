using System;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundIfExpression : BoundExpression
    {
        public BoundExpression Condition { get; }
        public BoundExpression Then { get; }
        public BoundExpression Else { get; }

        public BoundIfExpression(BoundExpression condition, BoundExpression then, BoundExpression @else)
        {
            Condition = condition;
            Then = then;
            Else = @else;

            ConstantValue = condition.ConstantValue == null
                ? null
                : (bool) condition.ConstantValue.Value ? then.ConstantValue : @else.ConstantValue;
        }

        public override BoundNodeKind Kind => BoundNodeKind.IfExpression;
        public override TypeSymbol Type => Then.Type;

        public override BoundConstant? ConstantValue { get; }
    }
}