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
        ErrorExpression,
        FieldExpression,
        ForExpression,
        IfExpression,
        LiteralExpression,
        NewExpression,
        MemberAssignmentExpression,
        MethodExpression,
        TypeExpression,
        UnaryExpression,
        UnitExpression,
        VariableExpression,
        WhileExpression,

        AssignmentStatement,
        ConditionalGotoStatement,
        ExpressionStatement,
        GotoStatement,
        LabelStatement,
        MemberAssignmentStatement,
        NopStatement,
        VariableDeclarationStatement,
    }
}