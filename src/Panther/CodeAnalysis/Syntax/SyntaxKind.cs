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
    CharToken,

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
    ThisKeyword,
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
    ArrayCreationExpression,
    AssignmentExpression,
    BinaryExpression,
    BlockExpression,
    CallExpression,
    ForExpression,
    GroupExpression,
    IfExpression,
    IndexExpression,
    LiteralExpression,
    MemberAccessExpression,
    NewExpression,
    ThisExpression,
    UnaryExpression,
    UnitExpression,
    WhileExpression,

    // Types
    QualifiedName,
    GenericName,
    IdentifierName,

    // Statements
    BreakStatement,
    ContinueStatement,
    ExpressionStatement,
    VariableDeclarationStatement,

    //  Nodes
    ArrayInitializer,
    CompilationUnit,
    FunctionBody,
    Initializer,
    Parameter,
    Template,
    TypeAnnotation,
    TypeArgumentList,

    // Members
    ClassDeclaration,
    FunctionDeclaration,
    ObjectDeclaration,

    // Top level items
    UsingDirective,
    GlobalStatement,
    NamespaceDeclaration,
}
