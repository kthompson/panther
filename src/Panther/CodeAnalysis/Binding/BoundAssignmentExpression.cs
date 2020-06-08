using System;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundAssignmentExpression : BoundExpression
    {
        public VariableSymbol Variable { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;

        public override TypeSymbol Type => TypeSymbol.Unit;

        public BoundAssignmentExpression(SyntaxNode syntax, VariableSymbol variable, BoundExpression expression) : base(syntax)
        {
            Variable = variable;
            Expression = expression;
        }
    }
}