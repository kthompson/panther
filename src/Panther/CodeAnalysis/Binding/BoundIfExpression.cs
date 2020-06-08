using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundIfExpression : BoundExpression
    {
        public BoundExpression Condition { get; }
        public BoundExpression Then { get; }
        public BoundExpression Else { get; }

        public BoundIfExpression(SyntaxNode syntax, BoundExpression condition, BoundExpression then, BoundExpression @else)
            : base(syntax)
        {
            Condition = condition;
            Then = then;
            Else = @else;

            ConstantValue = condition.ConstantValue == null
                ? null
                : (bool)condition.ConstantValue.Value ? then.ConstantValue : @else.ConstantValue;
        }

        public override BoundNodeKind Kind => BoundNodeKind.IfExpression;
        public override TypeSymbol Type => Then.Type;

        public override BoundConstant? ConstantValue { get; }
    }
}