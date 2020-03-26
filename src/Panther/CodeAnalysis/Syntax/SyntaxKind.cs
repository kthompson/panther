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
        WhileKeyword,

        // Operators
        PlusToken,

        MinusToken,
        SlashToken,
        StarToken,
        BangToken,
        AmpersandAmpersandToken,
        PipePipeToken,
        BangEqualsToken,
        EqualsToken,
        EqualsEqualsToken,
        LessThanToken,
        LessThanEqualsToken,
        GreaterThanToken,
        GreaterThanEqualsToken,

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

        // Statements

        VariableDeclarationStatement,
        ExpressionStatement,
        AssignmentStatement,

        //  Other
        CompilationUnit
    }
}