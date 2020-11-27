using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    /// <summary>
    /// Access a local variable
    /// </summary>
    internal record BoundVariableExpression(SyntaxNode Syntax, VariableSymbol Variable)
        : BoundExpression(Syntax)
    {
        public override TypeSymbol Type { get; init; } = Variable.Type;
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;
    }
}