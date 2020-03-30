using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    internal delegate ExpressionSyntax PrefixParseFunction();

    internal delegate ExpressionSyntax InfixParseFunction(ExpressionSyntax expression);

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
            //InfixParseFunctions[SyntaxKind.LeftBracket] = ParseIndexExpression;
        }

        private SyntaxToken PeekToken
        {
            get
            {
                var pos = _tokenPosition;

                TokenFromPosition(ref pos);
                pos++;
                return TokenFromPosition(ref pos);
            }
        }

        private SyntaxToken CurrentToken
        {
            get
            {
                var pos = _tokenPosition;
                return TokenFromPosition(ref pos);
            }
        }

        private SyntaxToken TokenFromPosition(ref int pos)
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

                return token;
            }
        }

        private void NextToken()
        {
            var pos = _tokenPosition++;
            TokenFromPosition(ref _tokenPosition);
        }

        private SyntaxToken Accept()
        {
            var token = CurrentToken;
            NextToken();
            return token;
        }

        private SyntaxToken AcceptStatementTerminator()
        {
            var currentToken = CurrentToken;
            switch (currentToken.Kind)
            {
                case SyntaxKind.NewLineToken:
                    return Accept();

                case SyntaxKind.EndOfInputToken:
                case SyntaxKind.CloseBraceToken:
                    // return end of file token but do not consume it
                    return currentToken;

                default:
                    Diagnostics.ReportUnexpectedToken(currentToken.Span, currentToken.Kind, SyntaxKind.NewLineToken);

                    return new SyntaxToken(SyntaxKind.NewLineToken, currentToken.Position);
            }
        }

        private SyntaxToken Accept(SyntaxKind kind)
        {
            var currentToken = CurrentToken;
            if (currentToken.Kind == kind)
                return Accept();

            Diagnostics.ReportUnexpectedToken(currentToken.Span, currentToken.Kind, kind);

            return new SyntaxToken(kind, currentToken.Position);
        }

        private void SkipNewLines()
        {
            while (CurrentToken.Kind == SyntaxKind.NewLineToken)
                Accept();
        }

        private SyntaxToken Create(SyntaxKind kind, string text)
        {
            var currentToken = CurrentToken;

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
            // advance until we get to the actual current token
            var currentToken = CurrentToken;

            var prefixFunction = _prefixParseFunctions.GetValueOrDefault(currentToken.Kind);
            if (prefixFunction == null)
            {
                // no prefix function
                // this results in an error from another location
                // so I don't think we need this here?
                // Diagnostics.ReportUnsupportedPrefixToken(currentToken);
                Diagnostics.ReportExpectedExpression(currentToken.Span, currentToken.Kind);
                return ParseLiteralExpression();
            }

            skipNewLines = currentToken.Kind != SyntaxKind.OpenBraceToken && skipNewLines;

            var left = prefixFunction();

            while (precedence < CurrentPrecedence())
            {
                var infix = _infixParseFunctions.GetValueOrDefault(CurrentToken.Kind);
                if (infix == null)
                    return left;

                left = infix(left);
            }

            return left;
        }

        private ExpressionSyntax ParsePrefixExpression()
        {
            // parse expressions like:
            // !b

            var unaryOperatorToken = Accept();

            var expression = ParseExpression(OperatorPrecedence.Prefix, false);

            return new UnaryExpressionSyntax(unaryOperatorToken, expression);
        }

        private OperatorPrecedence CurrentPrecedence() =>
            CurrentToken.Kind.GetBinaryOperatorPrecedence() ?? OperatorPrecedence.Lowest;

        private ExpressionSyntax ParseInfixExpression(ExpressionSyntax left)
        {
            // parse expressions like:
            // left + right
            var precedence = CurrentPrecedence();
            var binaryOperatorToken = Accept();
            SkipNewLines();
            var right = ParseExpression(precedence, true);

            return new BinaryExpressionSyntax(left, binaryOperatorToken, right);
        }

        private ExpressionSyntax ParseWhileExpression()
        {
            var whileKeyword = Accept();
            var openParenToken = Accept(SyntaxKind.OpenParenToken);
            var condition = ParseExpression(OperatorPrecedence.Lowest, false);
            var closeParenToken = Accept(SyntaxKind.CloseParenToken);
            var expr = ParseExpression(OperatorPrecedence.Lowest, false);

            return new WhileExpressionSyntax(whileKeyword, openParenToken, condition, closeParenToken, expr);
        }

        private ExpressionSyntax ParseForExpression()
        {
            var forKeyword = Accept();
            var openParenToken = Accept(SyntaxKind.OpenParenToken);
            var variable = ParseNameExpression(true);
            var leftArrow = Accept(SyntaxKind.LessThanDashToken);

            var fromExpression = ParseExpression(OperatorPrecedence.Lowest, false);
            var toKeyword = Accept(SyntaxKind.ToKeyword);
            var toExpression = ParseExpression(OperatorPrecedence.Lowest, false);
            var closeParenToken = Accept(SyntaxKind.CloseParenToken);

            var expr = ParseExpression(OperatorPrecedence.Lowest, false);

            return new ForExpressionSyntax(forKeyword, openParenToken, variable, leftArrow, fromExpression, toKeyword, toExpression, closeParenToken, expr);
        }

        private ExpressionSyntax ParseCallExpression()
        {
            var ident = Accept();
            var openParenToken = Accept(SyntaxKind.OpenParenToken);
            var arguments = ParseArguments();
            var closeParenToken = Accept(SyntaxKind.CloseParenToken);

            return new CallExpressionSyntax(ident, openParenToken, arguments, closeParenToken);
        }

        private SeparatedSyntaxList<ExpressionSyntax> ParseArguments()
        {
            var items = new List<SyntaxNode>();

            while (true)
            {
                var currentToken = CurrentToken;
                if (currentToken.Kind == SyntaxKind.CloseParenToken || currentToken.Kind == SyntaxKind.EndOfInputToken)
                    break;

                var arg = ParseExpression(OperatorPrecedence.Lowest, true);
                items.Add(arg);

                currentToken = CurrentToken;
                if (currentToken.Kind == SyntaxKind.CloseParenToken || currentToken.Kind == SyntaxKind.EndOfInputToken)
                    break;

                var comma = Accept(SyntaxKind.CommaToken);
                items.Add(comma);
            }

            return new SeparatedSyntaxList<ExpressionSyntax>(items.ToImmutableArray());
        }

        private ExpressionSyntax ParseIfExpression()
        {
            var ifKeyword = Accept();
            var openParenToken = Accept(SyntaxKind.OpenParenToken);
            SkipNewLines();
            var condition = ParseExpression(OperatorPrecedence.Lowest, false);
            SkipNewLines();
            var closeParenToken = Accept(SyntaxKind.CloseParenToken);
            SkipNewLines();
            var thenExpr = ParseExpression(OperatorPrecedence.Lowest, false);
            SkipNewLines();
            var elseKeyword = Accept(SyntaxKind.ElseKeyword);
            SkipNewLines();
            var elseExpr = ParseExpression(OperatorPrecedence.Lowest, false);

            return new IfExpressionSyntax(ifKeyword, openParenToken, condition, closeParenToken, thenExpr, elseKeyword, elseExpr);
        }

        private ExpressionSyntax ParseGroupOrUnitExpression()
        {
            var open = Accept();
            if (CurrentToken.Kind == SyntaxKind.CloseParenToken)
            {
                // unit expression
                var close = Accept(SyntaxKind.CloseParenToken);
                return new UnitExpressionSyntax(open, close);
            }
            else
            {
                var expr = ParseExpression(OperatorPrecedence.Lowest, true);
                var close = Accept(SyntaxKind.CloseParenToken);

                return new GroupExpressionSyntax(open, expr, close);
            }
        }

        private LiteralExpressionSyntax ParseLiteralExpression()
        {
            var numberToken = Accept();
            return new LiteralExpressionSyntax(numberToken);
        }

        private ExpressionSyntax ParseNameOrAssignmentExpression()
        {
            if (PeekTokenExact.Kind == SyntaxKind.EqualsToken)
                return ParseAssignmentExpression();

            if (PeekTokenExact.Kind == SyntaxKind.OpenParenToken)
                return ParseCallExpression();

            return ParseNameExpression(false);
        }

        private SyntaxToken PeekTokenExact => PeekToken;

        private NameExpressionSyntax ParseNameExpression(bool skipNewLines)
        {
            var token = Accept();

            return new NameExpressionSyntax(token);
        }

        private LiteralExpressionSyntax ParseBooleanLiteral()
        {
            var value = CurrentToken.Kind == SyntaxKind.TrueKeyword;
            return new LiteralExpressionSyntax(Accept(), value);
        }

        private ExpressionSyntax ParseBlockExpression()
        {
            var statements = new List<StatementSyntax>();
            var openBraceToken = Accept(SyntaxKind.OpenBraceToken);
            SkipNewLines();
            while (true)
            {
                var currentToken = CurrentToken;
                if (currentToken.Kind == SyntaxKind.EndOfInputToken || currentToken.Kind == SyntaxKind.CloseBraceToken)
                    break;

                statements.Add(ParseStatement());

                // prevent getting stuck in a loop as ParseStatement() does not always consume tokens
                if (currentToken == CurrentToken)
                    NextToken();
            }

            var expr = (statements.LastOrDefault() as ExpressionStatementSyntax)?.Expression;
            var stmts = expr == null ? statements : statements.Take(statements.Count - 1);

            if (expr == null)
            {
                var openParenToken = Create(SyntaxKind.OpenParenToken, "(");
                var closeParenToken = Create(SyntaxKind.CloseParenToken, ")");
                expr = new UnitExpressionSyntax(openParenToken, closeParenToken);
            }

            var closeBraceToken = Accept(SyntaxKind.CloseBraceToken);

            return new BlockExpressionSyntax(openBraceToken, stmts.ToImmutableArray(), expr, closeBraceToken);
        }

        private StatementSyntax ParseStatement()
        {
            switch (CurrentToken.Kind)
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
            var identToken = Accept(SyntaxKind.IdentifierToken);
            var equalsToken = Accept(SyntaxKind.EqualsToken);
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
            var valToken = Accept();
            var identToken = Accept(SyntaxKind.IdentifierToken);
            var equalsToken = Accept(SyntaxKind.EqualsToken);
            var expr = ParseExpression(OperatorPrecedence.Lowest, true);
            var newLineToken = AcceptStatementTerminator();

            return new VariableDeclarationStatementSyntax(valToken, identToken, equalsToken, expr, newLineToken);
        }
    }
}