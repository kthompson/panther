using System.Text;

namespace Panther.CodeAnalysis.Binding
{
    internal enum BoundNodeKind
    {
        AssignmentExpression,
        BinaryExpression,
        BlockExpression,
        ForExpression,
        IfExpression,
        LiteralExpression,
        UnaryExpression,
        UnitExpression,
        VariableExpression,
        WhileExpression,

        ConditionalGotoStatement,
        ExpressionStatement,
        GotoStatement,
        LabelStatement,
        VariableDeclarationStatement,
    }
}