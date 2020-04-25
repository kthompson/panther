using System;
using Panther.CodeAnalysis.Symbols;

namespace Panther.CodeAnalysis.Binding
{
    /// <summary>
    /// Access a local variable
    /// </summary>
    internal class BoundVariableExpression : BoundExpression
    {
        public VariableSymbol Variable { get; }
        public override TypeSymbol Type => Variable.Type;
        public override BoundNodeKind Kind => BoundNodeKind.VariableExpression;

        public BoundVariableExpression(VariableSymbol variable)
        {
            Variable = variable;
        }
    }
}