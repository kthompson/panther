namespace Panther.CodeAnalysis
{
    public enum SyntaxKind
    {
        // Special Tokens
        InvalidToken,

        EndOfInputToken,

        WhitespaceToken,
        NumberToken,

        // Operators
        PlusToken,

        MinusToken,
        SlashToken,
        StarToken,
        CloseParenToken,
        OpenParenToken,

        // Expressions
        LiteralExpression,

        BinaryExpression,
        GroupExpression,
        UnaryExpression
    }
}