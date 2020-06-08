using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundUnaryExpression : BoundExpression
    {
        public BoundUnaryExpression(SyntaxNode syntax, BoundUnaryOperator @operator, BoundExpression operand)
            : base(syntax)
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