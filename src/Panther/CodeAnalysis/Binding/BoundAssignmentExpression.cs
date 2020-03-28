using System;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundAssignmentExpression : BoundExpression
    {
        public VariableSymbol Variable { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;

        public override Type Type => typeof(Unit);

        public BoundAssignmentExpression(VariableSymbol variable, BoundExpression expression)
        {
            Variable = variable;
            Expression = expression;
        }
    }
}