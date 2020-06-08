using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal class BoundAssignmentStatement : BoundStatement
    {
        public VariableSymbol Variable { get; }
        public BoundExpression Expression { get; }
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;

        public BoundAssignmentStatement(SyntaxNode syntax, VariableSymbol variable, BoundExpression expression) : base(syntax)
        {
            Variable = variable;
            Expression = expression;
        }
    }
}