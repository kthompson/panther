using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public BoundUnaryExpression(BoundUnaryOperator @operator, BoundExpression operand)
        {
            Operator = @operator;
            Operand = operand;
            ConstantValue = ConstantFolding.ComputeConstant(@operator, operand);
        }

        public BoundUnaryOperator Operator { get; }

        public BoundExpression Operand { get; }

        public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
        public override TypeSymbol Type => Operator.Type;
        public override BoundConstant? ConstantValue { get; }
    }
}