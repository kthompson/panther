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

        private readonly Dictionary<SyntaxKind, PrefixParseFunction> PrefixParseFunctions = new Dictionary<SyntaxKind, PrefixParseFunction>();

        private readonly Dictionary<SyntaxKind, InfixParseFunction> InfixParseFunctions =
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
            PrefixParseFunctions[SyntaxKind.IdentifierToken] = ParseIdentifierExpression;
            PrefixParseFunctions[SyntaxKind.NumberToken] = ParseIntegerLiteralExpression;
            PrefixParseFunctions[SyntaxKind.TrueKeyword] = ParseBooleanLiteral;
            PrefixParseFunctions[SyntaxKind.FalseKeyword] = ParseBooleanLiteral;
            PrefixParseFunctions[SyntaxKind.BangToken] = ParsePrefixExpression;
            PrefixParseFunctions[SyntaxKind.MinusToken] = ParsePrefixExpression;
            PrefixParseFunctions[SyntaxKind.PlusToken] = ParsePrefixExpression;
            PrefixParseFunctions[SyntaxKind.OpenParenToken] = ParseGroupExpression;
            //PrefixParseFunctions[SyntaxKind.If] = ParseIfExpression;
            //PrefixParseFunctions[SyntaxKind.Function] = ParseFunctionLiteral;
            //PrefixParseFunctions[SyntaxKind.String] = ParseStringLiteral;
            //PrefixParseFunctions[SyntaxKind.LeftBracket] = ParseArrayLiteral;
            PrefixParseFunctions[SyntaxKind.OpenBraceToken] = ParseBlockExpression;

            // a + b
            InfixParseFunctions[SyntaxKind.AmpersandAmpersandToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.PipePipeToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.EqualsEqualsToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.BangEqualsToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.LessThanToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.GreaterThanToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.LessThanEqualsToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.GreaterThanEqualsToken] = ParseInfixExpression;

            InfixParseFunctions[SyntaxKind.PlusToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.MinusToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.SlashToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.StarToken] = ParseInfixExpression;
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
            var pos = _tokenPosition;

            return TokenFromPosition(skipNewLines, ref pos);
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

                    return new SyntaxToken(SyntaxKind.NewLineToken, currentToken.Position, string.Empty, null);
            }
        }

        private SyntaxToken Accept(SyntaxKind kind, bool skipNewLines = false)
        {
            var currentToken = CurrentToken(skipNewLines);
            if (currentToken.Kind == kind)
                return Accept(skipNewLines);

            Diagnostics.ReportUnexpectedToken(currentToken.Span, currentToken.Kind, kind);

            return new SyntaxToken(kind, currentToken.Position, string.Empty, null);
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
            var prefixFunction = PrefixParseFunctions.GetValueOrDefault(currentToken.Kind);
            if (prefixFunction == null)
            {
                // no prefix function
                throw new Exception($"missing prefix function for {currentToken.Kind}");
                return null;
            }

            skipNewLines = currentToken.Kind != SyntaxKind.OpenBraceToken && skipNewLines;

            var left = prefixFunction(skipNewLines);

            while (precedence < CurrentPrecedence())
            {
                var infix = InfixParseFunctions.GetValueOrDefault(CurrentToken(skipNewLines).Kind);
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
            var right = ParseExpression(precedence, skipNewLines);

            return new BinaryExpressionSyntax(left, binaryOperatorToken, right);
        }

        private ExpressionSyntax ParseGroupExpression(bool skipNewLines)
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

        private LiteralExpressionSyntax ParseIntegerLiteralExpression(bool skipNewLines)
        {
            var numberToken = Accept(false);
            return new LiteralExpressionSyntax(numberToken);
        }

        private NameExpressionSyntax ParseIdentifierExpression(bool skipNewLines)
        {
            var token = Accept(false);

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

            while (CurrentToken(skipNewLines).Kind != SyntaxKind.EndOfInputToken && CurrentToken(skipNewLines).Kind != SyntaxKind.CloseBraceToken)
            {
                statements.Add(ParseStatement());
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

                case SyntaxKind.IdentifierToken when PeekToken(false).Kind == SyntaxKind.EqualsToken:
                    return ParseAssignmentStatement();

                default:
                    return ParseExpressionStatement();
            }
        }

        private StatementSyntax ParseAssignmentStatement()
        {
            var identToken = Accept(SyntaxKind.IdentifierToken, false);
            var equalsToken = Accept(SyntaxKind.EqualsToken, false);
            var expr = ParseExpression(OperatorPrecedence.Lowest, false);
            var newLineToken = AcceptStatementTerminator();

            return new AssignmentStatementSyntax(identToken, equalsToken, expr, newLineToken);
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