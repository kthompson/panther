using System.Text;

namespace Panther.CodeAnalysis.Binding
{
    internal enum BoundNodeKind
    {
        UnaryExpression,
        LiteralExpression,
        BinaryExpression,
        VariableExpression,
        BlockExpression,
        UnitExpression,
        IfExpression,
        WhileExpression,
        ForExpression,

        ExpressionStatement,
        VariableDeclarationStatement,
        AssignmentExpression
    }
}