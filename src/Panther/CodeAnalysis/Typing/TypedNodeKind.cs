namespace Panther.CodeAnalysis.Typing;

internal enum TypedNodeKind
{
    // Expressions
    ArrayCreationExpression,
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
    IndexExpression,
    LiteralExpression,
    MethodExpression,
    NamespaceExpression,
    NewExpression,
    NullExpression,
    PropertyExpression,
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
    ObjectDeclaration,
}
