using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal record BoundAssignmentExpression(SyntaxNode Syntax, VariableSymbol Variable, BoundExpression Expression)
        : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
        public override TypeSymbol Type { get ; init; } = TypeSymbol.Unit;
    }
}