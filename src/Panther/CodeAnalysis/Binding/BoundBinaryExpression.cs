using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal sealed class BoundBinaryExpression : BoundExpression
    {
        public BoundBinaryExpression(SyntaxNode syntax, BoundExpression left, BoundBinaryOperator @operator, BoundExpression right)
            : base(syntax)
        {
            Operator = @operator;
            Left = left;
            Right = right;
            ConstantValue = ConstantFolding.ComputeConstant(left, @operator, right);
        }

        public BoundExpression Left { get; }

        public BoundBinaryOperator Operator { get; }

        public BoundExpression Right { get; }

        public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
        public override TypeSymbol Type => Operator.Type;
        public override BoundConstant? ConstantValue { get; }
    }
}