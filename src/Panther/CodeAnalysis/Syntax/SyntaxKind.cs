namespace Panther.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        // Special Tokens
        InvalidToken,

        EndOfInputToken,
        NewLineToken,
        WhitespaceToken,
        NumberToken,
        IdentifierToken,

        // Keywords
        TrueKeyword,

        FalseKeyword,
        ValKeyword,
        VarKeyword,
        IfKeyword,
        ElseKeyword,
        WhileKeyword,
        ForKeyword,
        ToKeyword,

        // Operators
        PlusToken,

        DashToken,
        SlashToken,
        StarToken,
        BangToken,
        AmpersandToken,
        AmpersandAmpersandToken,
        PipePipeToken,
        PipeToken,
        BangEqualsToken,
        EqualsToken,
        EqualsEqualsToken,
        LessThanToken,
        LessThanEqualsToken,
        GreaterThanToken,
        GreaterThanEqualsToken,
        CaretToken,
        TildeToken,
        LessThanDashToken,

        // grouping tokens
        CloseParenToken,

        OpenParenToken,
        OpenBraceToken,
        CloseBraceToken,

        // Expressions
        LiteralExpression,

        BinaryExpression,
        GroupExpression,
        UnaryExpression,
        NameExpression,
        UnitExpression,
        BlockExpression,
        IfExpression,
        WhileExpression,
        ForExpression,

        // Statements

        VariableDeclarationStatement,
        ExpressionStatement,
        AssignmentExpression,

        //  Other
        CompilationUnit
    }
}