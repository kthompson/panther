using System;
using System.Collections.Immutable;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundBlockExpression : BoundExpression
    {
        public ImmutableArray<BoundStatement> Statements { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.BlockExpression;
        public override Type Type => Expression?.Type;

        public BoundBlockExpression(ImmutableArray<BoundStatement> statements, BoundExpression expression)
        {
            Statements = statements;
            Expression = expression;
        }
    }
}