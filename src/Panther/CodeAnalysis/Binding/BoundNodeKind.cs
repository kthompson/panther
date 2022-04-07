using System.Text;

namespace Panther.CodeAnalysis.Binding;

internal enum BoundNodeKind
{
    // Expressions
    AssignmentExpression,
    BinaryExpression,
    BlockExpression,
    CallExpression,
    ConversionExpression,
    ErrorExpression,
    FieldExpression,
    ForExpression,
    GroupExpression,
    IfExpression,
    LiteralExpression,
    MethodExpression,
    NamespaceExpression,
    NewExpression,
    TypeExpression,
    UnaryExpression,
    UnitExpression,
    VariableExpression,
    WhileExpression,

    // Statements
    AssignmentStatement,
    BreakStatement,
    ConditionalGotoStatement,
    ContinueStatement,
    ExpressionStatement,
    GlobalStatement,
    GotoStatement,
    LabelStatement,
    MemberAssignmentStatement,
    NopStatement,
    VariableDeclarationStatement,

    // Declarations
    ClassDeclaration,
    FunctionDeclaration,
    ObjectDeclaration
}