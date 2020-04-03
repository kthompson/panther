namespace Panther.CodeAnalysis.Syntax
{
    public enum SyntaxKind
    {
        // Special Tokens
        InvalidToken,

        EndOfInputToken,
        IdentifierToken,
        CommaToken,

        // Whitespace tokens
        NewLineToken,

        WhitespaceToken,

        // Literal tokens
        NumberToken,

        StringToken,

        // Keywords
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
        ColonToken,

        // grouping tokens
        CloseParenToken,

        OpenParenToken,
        OpenBraceToken,
        CloseBraceToken,

        // Expressions
        BinaryExpression,

        BlockExpression,
        CallExpression,
        ForExpression,
        GroupExpression,
        IfExpression,
        LiteralExpression,
        NameExpression,
        UnaryExpression,
        UnitExpression,
        WhileExpression,

        // Statements
        VariableDeclarationStatement,

        ExpressionStatement,
        AssignmentExpression,

        //  Nodes
        TypeClause,

        Parameter,
        CompilationUnit,

        // Members
        FunctionDeclaration,

        GlobalStatement
    }
}