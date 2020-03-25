namespace Panther.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        // Special Tokens
        InvalidToken,

        EndOfInputToken,
        WhitespaceToken,
        NumberToken,
        IdentifierToken,

        // Keywords
        TrueKeyword,

        FalseKeyword,
        ValKeyword,
        VarKeyword,

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
        AssignmentStatement,
        // Statements

        BlockStatement,
        ExpressionStatement,

        //  Other
        CompilationUnit
    }
}