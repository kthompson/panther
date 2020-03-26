﻿using System;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundVariableDeclarationStatement : BoundStatement
    {
        public VariableSymbol Variable { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.VariableDeclarationStatement;

        public BoundVariableDeclarationStatement(VariableSymbol variable, BoundExpression expression)
        {
            Variable = variable;
            Expression = expression;
        }
    }
}