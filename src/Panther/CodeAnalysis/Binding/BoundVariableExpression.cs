using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

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

        public BoundVariableExpression(SyntaxNode syntax, VariableSymbol variable)
            : base(syntax)
        {
            Variable = variable;
        }
    }
}