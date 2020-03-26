﻿namespace Panther.CodeAnalysis.Binding
{
    internal class BoundAssignmentStatement : BoundStatement
    {
        public VariableSymbol Variable { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;

        public BoundAssignmentStatement(VariableSymbol variable, BoundExpression expression)
        {
            Variable = variable;
            Expression = expression;
        }
    }
}