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

        // Expressions
        LiteralExpression,

        BinaryExpression,
        GroupExpression,
        UnaryExpression,
        NameExpression,
        AssignmentExpression,

        //  Other
        CompilationUnit
    }
}