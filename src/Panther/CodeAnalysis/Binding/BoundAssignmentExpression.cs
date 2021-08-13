using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;

namespace Panther.CodeAnalysis.Binding
{
    internal record BoundAssignmentExpression(SyntaxNode Syntax, Symbol Variable, BoundExpression Expression)
        : BoundExpression(Syntax)
    {
        public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
        public override Type Type { get ; init; } = Type.Unit;
    }
}