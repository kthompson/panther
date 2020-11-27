using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal record BoundIfExpression(SyntaxNode Syntax, BoundExpression Condition, BoundExpression Then,
        BoundExpression Else) : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.IfExpression;
        public override TypeSymbol Type { get; init; } = Then.Type;

        public override BoundConstant? ConstantValue { get; } =
            Condition.ConstantValue == null
                ? null
                : (bool) Condition.ConstantValue.Value
                    ? Then.ConstantValue
                    : Else.ConstantValue;
    }
}