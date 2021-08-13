using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal record BoundAssignmentStatement(SyntaxNode Syntax, Symbol Variable, BoundExpression Expression)
        : BoundStatement(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentStatement;
    }
}