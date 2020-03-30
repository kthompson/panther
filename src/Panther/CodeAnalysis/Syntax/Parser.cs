using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    internal delegate ExpressionSyntax PrefixParseFunction(bool skipNewLines);

    internal delegate ExpressionSyntax InfixParseFunction(ExpressionSyntax expression, bool skipNewLines);

    //internal enum OperatorPrecedence
    //{
    //    Lowest,
    //    Equals, // ==
    //    LessGreater, // > or <
    //    Sum, // +
    //    Product, // *
    //    Prefix, // -X or !X
    //    Call, // myFunction(X)
    //    Index // myArray[x]
    //}

    internal class Parser
    {
        public DiagnosticBag Diagnostics { get; } = new DiagnosticBag();

        private readonly Dictionary<SyntaxKind, PrefixParseFunction> _prefixParseFunctions = new Dictionary<SyntaxKind, PrefixParseFunction>();

        private readonly Dictionary<SyntaxKind, InfixParseFunction> _infixParseFunctions =
            new Dictionary<SyntaxKind, InfixParseFunction>();

        private ImmutableArray<SyntaxToken> _tokens;
        private int _tokenPosition = 0;

        public Parser(SourceText text)
            : this(new Lexer(text))
        {
        }

        public Parser(Lexer lexer)
        {
            var tokens = new List<SyntaxToken>();
            while (true)
            {
                var token = lexer.NextToken();
                tokens.Add(token);
                if (token.Kind == SyntaxKind.EndOfInputToken)
                {
                    break;
                }
            }

            _tokens = tokens.ToImmutableArray();

            this.Diagnostics.AddRange(lexer.Diagnostics);

            // -a
            _prefixParseFunctions[SyntaxKind.BangToken] = ParsePrefixExpression;
            _prefixParseFunctions[SyntaxKind.DashToken] = ParsePrefixExpression;
            _prefixParseFunctions[SyntaxKind.PlusToken] = ParsePrefixExpression;
            _prefixParseFunctions[SyntaxKind.TildeToken] = ParsePrefixExpression;

            _prefixParseFunctions[SyntaxKind.IdentifierToken] = ParseNameOrAssignmentExpression;
            _prefixParseFunctions[SyntaxKind.StringToken] = ParseLiteralExpression;
            _prefixParseFunctions[SyntaxKind.NumberToken] = ParseLiteralExpression;
            _prefixParseFunctions[SyntaxKind.TrueKeyword] = ParseBooleanLiteral;
            _prefixParseFunctions[SyntaxKind.FalseKeyword] = ParseBooleanLiteral;
            _prefixParseFunctions[SyntaxKind.OpenParenToken] = ParseGroupOrUnitExpression;
            _prefixParseFunctions[SyntaxKind.IfKeyword] = ParseIfExpression;
            _prefixParseFunctions[SyntaxKind.WhileKeyword] = ParseWhileExpression;
            _prefixParseFunctions[SyntaxKind.ForKeyword] = ParseForExpression;
            //PrefixParseFunctions[SyntaxKind.Function] = ParseFunctionLiteral;
            //PrefixParseFunctions[SyntaxKind.String] = ParseStringLiteral;
            //PrefixParseFunctions[SyntaxKind.LeftBracket] = ParseArrayLiteral;
            _prefixParseFunctions[SyntaxKind.OpenBraceToken] = ParseBlockExpression;

            // a + b
            _infixParseFunctions[SyntaxKind.AmpersandAmpersandToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.AmpersandToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.BangEqualsToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.CaretToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.EqualsEqualsToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.GreaterThanEqualsToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.GreaterThanToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.LessThanEqualsToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.LessThanToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.DashToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.PipePipeToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.PipeToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.PlusToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.SlashToken] = ParseInfixExpression;
            _infixParseFunctions[SyntaxKind.StarToken] = ParseInfixExpression;
            //InfixParseFunctions[SyntaxKind.OpenParenToken] = ParseCallExpression;
            //InfixParseFunctions[SyntaxKind.LeftBracket] = ParseIndexExpression;
        }

        private SyntaxToken PeekToken(bool skipNewLines)
        {
            var pos = _tokenPosition;

            TokenFromPosition(skipNewLines, ref pos);
            pos++;
            return TokenFromPosition(skipNewLines, ref pos);
        }

        private SyntaxToken CurrentToken(bool skipNewLines)
        {
            return TokenFromPosition(skipNewLines, ref _tokenPosition);
        }

        private SyntaxToken TokenFromPosition(bool skipNewLines, ref int pos)
        {
            while (true)
            {
                if (pos > _tokens.Length - 1)
                    return _tokens[^1];

                var token = _tokens[pos];

                if (token.Kind == SyntaxKind.WhitespaceToken || token.Kind == SyntaxKind.InvalidToken)
                {
                    pos++;
                    continue;
                }

                if (skipNewLines && token.Kind == SyntaxKind.NewLineToken)
                {
                    pos++;
                    continue;
                }

                return token;
            }
        }

        private void NextToken(bool skipNewLines)
        {
            _tokenPosition++;
            TokenFromPosition(skipNewLines, ref _tokenPosition);
        }

        private SyntaxToken Accept(bool skipNewLines)
        {
            var token = CurrentToken(skipNewLines);
            NextToken(skipNewLines);
            return token;
        }

        private SyntaxToken AcceptStatementTerminator(bool skipNewLines = false)
        {
            var currentToken = CurrentToken(skipNewLines);
            switch (currentToken.Kind)
            {
                case SyntaxKind.NewLineToken:
                    return Accept(skipNewLines);

                case SyntaxKind.EndOfInputToken:
                case SyntaxKind.CloseBraceToken:
                    // return end of file token but do not consume it
                    return currentToken;

                default:
                    Diagnostics.ReportUnexpectedToken(currentToken.Span, currentToken.Kind, SyntaxKind.NewLineToken);

                    return new SyntaxToken(SyntaxKind.NewLineToken, currentToken.Position, string.Empty, null, true);
            }
        }

        private SyntaxToken Accept(SyntaxKind kind, bool skipNewLines = false)
        {
            var currentToken = TokenFromPosition(skipNewLines, ref _tokenPosition);
            if (currentToken.Kind == kind)
                return Accept(skipNewLines);

            Diagnostics.ReportUnexpectedToken(currentToken.Span, currentToken.Kind, kind);

            return new SyntaxToken(kind, currentToken.Position, string.Empty, null, true);
        }

        private SyntaxToken Create(SyntaxKind kind, string text, bool skipNewLines = false)
        {
            var currentToken = CurrentToken(skipNewLines);

            return new SyntaxToken(kind, currentToken.Position, text, null);
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            var statement = ParseStatement();

            var endToken = Accept(SyntaxKind.EndOfInputToken);

            return new CompilationUnitSyntax(statement, endToken);
        }

        private ExpressionSyntax ParseExpression(OperatorPrecedence precedence, bool skipNewLines)
        {
            var currentToken = CurrentToken(skipNewLines);
            var prefixFunction = _prefixParseFunctions.GetValueOrDefault(currentToken.Kind);
            if (prefixFunction == null)
            {
                // no prefix function
                // this results in an error from another location
                // so I don't think we need this here?
                // Diagnostics.ReportUnsupportedPrefixToken(currentToken);
                Diagnostics.ReportExpectedExpression(currentToken.Span, currentToken.Kind);
                return ParseLiteralExpression(false);
            }

            skipNewLines = currentToken.Kind != SyntaxKind.OpenBraceToken && skipNewLines;

            var left = prefixFunction(skipNewLines);

            while (precedence < CurrentPrecedence())
            {
                var infix = _infixParseFunctions.GetValueOrDefault(CurrentToken(skipNewLines).Kind);
                if (infix == null)
                    return left;

                left = infix(left, skipNewLines);
            }

            return left;
        }

        private ExpressionSyntax ParsePrefixExpression(bool skipNewLines)
        {
            // parse expressions like:
            // !b

            var unaryOperatorToken = Accept(false);

            var expression = ParseExpression(OperatorPrecedence.Prefix, skipNewLines);

            return new UnaryExpressionSyntax(unaryOperatorToken, expression);
        }

        private OperatorPrecedence CurrentPrecedence() =>
            CurrentToken(false).Kind.GetBinaryOperatorPrecedence() ?? OperatorPrecedence.Lowest;

        private ExpressionSyntax ParseInfixExpression(ExpressionSyntax left, bool skipNewLines)
        {
            // parse expressions like:
            // left + right
            var precedence = CurrentPrecedence();
            var binaryOperatorToken = Accept(false);
            var right = ParseExpression(precedence, true);

            return new BinaryExpressionSyntax(left, binaryOperatorToken, right);
        }

        private ExpressionSyntax ParseWhileExpression(bool skipNewLines)
        {
            var whileKeyword = Accept(false);
            var openParenToken = Accept(SyntaxKind.OpenParenToken, true);
            var condition = ParseExpression(OperatorPrecedence.Lowest, false);
            var closeParenToken = Accept(SyntaxKind.CloseParenToken, true);
            var expr = ParseExpression(OperatorPrecedence.Lowest, false);

            return new WhileExpressionSyntax(whileKeyword, openParenToken, condition, closeParenToken, expr);
        }

        private ExpressionSyntax ParseForExpression(bool skipnewlines)
        {
            var forKeyword = Accept(false);
            var openParenToken = Accept(SyntaxKind.OpenParenToken, true);
            var variable = ParseNameExpression(true);
            var leftArrow = Accept(SyntaxKind.LessThanDashToken, false);

            var fromExpression = ParseExpression(OperatorPrecedence.Lowest, false);
            var toKeyword = Accept(SyntaxKind.ToKeyword, true);
            var toExpression = ParseExpression(OperatorPrecedence.Lowest, false);
            var closeParenToken = Accept(SyntaxKind.CloseParenToken, true);

            var expr = ParseExpression(OperatorPrecedence.Lowest, false);

            return new ForExpressionSyntax(forKeyword, openParenToken, variable, leftArrow, fromExpression, toKeyword, toExpression, closeParenToken, expr);
        }

        private ExpressionSyntax ParseIfExpression(bool skipnewlines)
        {
            var ifKeyword = Accept(false);
            var openParenToken = Accept(SyntaxKind.OpenParenToken, true);
            var condition = ParseExpression(OperatorPrecedence.Lowest, false);
            var closeParenToken = Accept(SyntaxKind.CloseParenToken, true);
            var thenExpr = ParseExpression(OperatorPrecedence.Lowest, false);
            var elseKeyword = Accept(SyntaxKind.ElseKeyword, true);
            var elseExpr = ParseExpression(OperatorPrecedence.Lowest, false);

            return new IfExpressionSyntax(ifKeyword, openParenToken, condition, closeParenToken, thenExpr, elseKeyword, elseExpr);
        }

        private ExpressionSyntax ParseGroupOrUnitExpression(bool skipNewLines)
        {
            var open = Accept(false);
            if (CurrentToken(skipNewLines).Kind == SyntaxKind.CloseParenToken)
            {
                // unit expression
                var close = Accept(SyntaxKind.CloseParenToken, false);
                return new UnitExpressionSyntax(open, close);
            }
            else
            {
                var expr = ParseExpression(OperatorPrecedence.Lowest, skipNewLines);
                var close = Accept(SyntaxKind.CloseParenToken, false);

                return new GroupExpressionSyntax(open, expr, close);
            }
        }

        private LiteralExpressionSyntax ParseLiteralExpression(bool skipNewLines)
        {
            var numberToken = Accept(false);
            return new LiteralExpressionSyntax(numberToken);
        }

        private ExpressionSyntax ParseNameOrAssignmentExpression(bool skipNewLines)
        {
            if (PeekToken(false).Kind == SyntaxKind.EqualsToken)
                return ParseAssignmentExpression();

            return ParseNameExpression(false);
        }

        private NameExpressionSyntax ParseNameExpression(bool skipNewLines)
        {
            var token = Accept(skipNewLines);

            return new NameExpressionSyntax(token);
        }

        private LiteralExpressionSyntax ParseBooleanLiteral(bool skipNewLines)
        {
            var value = CurrentToken(skipNewLines).Kind == SyntaxKind.TrueKeyword;
            return new LiteralExpressionSyntax(Accept(false), value);
        }

        private ExpressionSyntax ParseBlockExpression(bool skipNewLines)
        {
            var statements = new List<StatementSyntax>();
            var openBraceToken = Accept(SyntaxKind.OpenBraceToken, true);

            while (true)
            {
                var currentToken = CurrentToken(skipNewLines);
                if (currentToken.Kind == SyntaxKind.EndOfInputToken || currentToken.Kind == SyntaxKind.CloseBraceToken)
                    break;

                statements.Add(ParseStatement());

                // prevent getting stuck in a loop as ParseStatement() does not always consume tokens
                if (currentToken == CurrentToken(skipNewLines))
                    NextToken(skipNewLines);
            }

            var expr = (statements.LastOrDefault() as ExpressionStatementSyntax)?.Expression;
            var stmts = expr == null ? statements : statements.Take(statements.Count - 1);

            if (expr == null)
            {
                var openParenToken = Create(SyntaxKind.OpenParenToken, "(", false);
                var closeParenToken = Create(SyntaxKind.CloseParenToken, ")", false);
                expr = new UnitExpressionSyntax(openParenToken, closeParenToken);
            }

            var closeBraceToken = Accept(SyntaxKind.CloseBraceToken, false);

            return new BlockExpressionSyntax(openBraceToken, stmts.ToImmutableArray(), expr, closeBraceToken);
        }

        private StatementSyntax ParseStatement()
        {
            switch (CurrentToken(false).Kind)
            {
                case SyntaxKind.ValKeyword:
                case SyntaxKind.VarKeyword:
                    return ParseVariableDeclarationStatement();

                default:
                    return ParseExpressionStatement();
            }
        }

        private ExpressionSyntax ParseAssignmentExpression()
        {
            var identToken = Accept(SyntaxKind.IdentifierToken, false);
            var equalsToken = Accept(SyntaxKind.EqualsToken, false);
            var expr = ParseExpression(OperatorPrecedence.Lowest, true);

            return new AssignmentExpressionSyntax(identToken, equalsToken, expr);
        }

        private StatementSyntax ParseExpressionStatement()
        {
            var expr = ParseExpression(OperatorPrecedence.Lowest, false);
            var newLineToken = AcceptStatementTerminator();
            return new ExpressionStatementSyntax(expr, newLineToken);
        }

        private StatementSyntax ParseVariableDeclarationStatement()
        {
            var valToken = Accept(false);
            var identToken = Accept(SyntaxKind.IdentifierToken, false);
            var equalsToken = Accept(SyntaxKind.EqualsToken, false);
            var expr = ParseExpression(OperatorPrecedence.Lowest, false);
            var newLineToken = AcceptStatementTerminator();

            return new VariableDeclarationStatementSyntax(valToken, identToken, equalsToken, expr, newLineToken);
        }
    }
}