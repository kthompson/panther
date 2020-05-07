using System.Text;

namespace Panther.CodeAnalysis.Binding
{
    internal enum BoundNodeKind
    {
        AssignmentExpression,
        BinaryExpression,
        BlockExpression,
        CallExpression,
        ConversionExpression,
        ForExpression,
        IfExpression,
        LiteralExpression,
        UnaryExpression,
        UnitExpression,
        VariableExpression,
        WhileExpression,
        ErrorExpression,

        AssignmentStatement,
        ConditionalGotoStatement,
        ExpressionStatement,
        GotoStatement,
        LabelStatement,
        NopStatement,
        VariableDeclarationStatement,
    }
}