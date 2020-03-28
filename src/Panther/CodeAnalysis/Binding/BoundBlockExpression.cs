using System;
using System.Collections.Immutable;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundBlockExpression : BoundExpression
    {
        public ImmutableArray<BoundStatement> Statements { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.BlockExpression;
        public override TypeSymbol Type => Expression.Type;

        public BoundBlockExpression(ImmutableArray<BoundStatement> statements, BoundExpression expression)
        {
            Statements = statements;
            Expression = expression;
        }
    }
}