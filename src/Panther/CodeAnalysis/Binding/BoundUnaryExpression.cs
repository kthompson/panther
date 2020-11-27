using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed record BoundUnaryExpression(SyntaxNode Syntax, BoundUnaryOperator Operator, BoundExpression Operand)
        : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override TypeSymbol Type { get ; init; } = Operator.Type;
        public override BoundConstant? ConstantValue { get; } = ConstantFolding.ComputeConstant(Operator, Operand);
    }
}