﻿using System;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundIfExpression : BoundExpression
    {
        public BoundExpression Condition { get; }
        public BoundExpression Then { get; }
        public BoundExpression Else { get; }

        public BoundIfExpression(BoundExpression condition, BoundExpression then, BoundExpression @else)
        {
            Condition = condition;
            Then = then;
            Else = @else;
        }

        public override BoundNodeKind Kind => BoundNodeKind.IfExpression;
        public override Type Type => Then.Type;
    }
}