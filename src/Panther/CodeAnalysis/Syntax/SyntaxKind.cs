namespace Panther.CodeAnalysis.Syntax;

public enum SyntaxKind
{
    // Special Tokens
    EndOfInputToken,
    IdentifierToken,
    CommaToken,

    // Trivia tokens
    InvalidTokenTrivia,
    EndOfLineTrivia,
    WhitespaceTrivia,
    LineCommentTrivia,
    BlockCommentTrivia,

    // Literal tokens
    NumberToken,
    StringToken,

    // Keywords
    BreakKeyword,
    ClassKeyword,
    ContinueKeyword,
    DefKeyword,
    ElseKeyword,
    FalseKeyword,
    ForKeyword,
    IfKeyword,
    ImplicitKeyword,
    NamespaceKeyword,
    NewKeyword,
    ObjectKeyword,
    StaticKeyword,
    ToKeyword,
    TrueKeyword,
    UsingKeyword,
    ValKeyword,
    VarKeyword,
    WhileKeyword,

    // Operators
    AmpersandAmpersandToken,
    AmpersandToken,
    BangEqualsToken,
    BangToken,
    CaretToken,
    ColonToken,
    DashToken,
    DotToken,
    EqualsEqualsToken,
    EqualsToken,
    GreaterThanEqualsToken,
    GreaterThanToken,
    LessThanDashToken,
    LessThanEqualsToken,
    LessThanToken,
    PipePipeToken,
    PipeToken,
    PlusToken,
    SlashToken,
    StarToken,
    TildeToken,

    // grouping tokens
    CloseParenToken,
    OpenParenToken,
    OpenBraceToken,
    CloseBraceToken,
    OpenBracketToken,
    CloseBracketToken,

    // Expressions
    AssignmentExpression,
    BinaryExpression,
    BlockExpression,
    CallExpression,
    ForExpression,
    GroupExpression,
    IdentifierName,
    IfExpression,
    IndexExpression,
    LiteralExpression,
    MemberAccessExpression,
    NewExpression,
    QualifiedName,
    UnaryExpression,
    UnitExpression,
    WhileExpression,

    // Statements
    BreakStatement,
    ContinueStatement,
    ExpressionStatement,
    VariableDeclarationStatement,

    //  Nodes
    Template,
    TypeAnnotation,
    FunctionBody,
    Parameter,
    Initializer,
    CompilationUnit,

    // Members
    ClassDeclaration,
    FunctionDeclaration,
    ObjectDeclaration,

    // Top level items
    UsingDirective,
    GlobalStatement,
    NamespaceDeclaration,
}
