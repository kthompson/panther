using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundBinaryExpression(SyntaxNode Syntax, BoundExpression Left,
        BoundBinaryOperator Operator,
        BoundExpression Right) : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
        public override TypeSymbol Type { get ; init; } = Operator.Type;
        public override BoundConstant? ConstantValue { get; } = ConstantFolding.ComputeConstant(Left, Operator, Right);
    }
}