namespace Panther.CodeAnalysis.Syntax
{
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
        ContinueKeyword,
        DefKeyword,
        ElseKeyword,
        FalseKeyword,
        ForKeyword,
        IfKeyword,
        ToKeyword,
        TrueKeyword,
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

        // Expressions
        AssignmentExpression,
        BinaryExpression,
        BlockExpression,
        BreakExpression,
        CallExpression,
        ContinueExpression,
        ForExpression,
        GroupExpression,
        IfExpression,
        LiteralExpression,
        NameExpression,
        UnaryExpression,
        UnitExpression,
        WhileExpression,

        // Statements
        ExpressionStatement,
        VariableDeclarationStatement,

        //  Nodes
        TypeAnnotation,
        Parameter,
        CompilationUnit,

        // Members
        FunctionDeclaration,
        GlobalStatement
    }
}