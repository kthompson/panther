using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal record BoundAssignmentStatement(SyntaxNode Syntax, VariableSymbol Variable, BoundExpression Expression)
        : BoundStatement(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;
    }
}