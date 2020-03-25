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
        private readonly Lexer _lexer;
        private SyntaxToken _previousToken;
        private SyntaxToken _currentToken;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        public ImmutableArray<Diagnostic> Diagnostics => new DiagnosticBag().AddRange(_lexer.Diagnostics).AddRange(_diagnostics).ToImmutableArray();

        private readonly Dictionary<SyntaxKind, PrefixParseFunction> PrefixParseFunctions = new Dictionary<SyntaxKind, PrefixParseFunction>();

        private readonly Dictionary<SyntaxKind, InfixParseFunction> InfixParseFunctions =
            new Dictionary<SyntaxKind, InfixParseFunction>();

        public Parser(SourceText text)
            : this(new Lexer(text))
        {
        }

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
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
            //InfixParseFunctions[SyntaxKind.LessThan] = ParseInfixExpression;
            //InfixParseFunctions[SyntaxKind.GreaterThan] = ParseInfixExpression;

            InfixParseFunctions[SyntaxKind.PlusToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.MinusToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.SlashToken] = ParseInfixExpression;
            InfixParseFunctions[SyntaxKind.StarToken] = ParseInfixExpression;
            //InfixParseFunctions[SyntaxKind.OpenParenToken] = ParseCallExpression;
            //InfixParseFunctions[SyntaxKind.LeftBracket] = ParseIndexExpression;

            // init first token
            _currentToken = NextToken(false);
        }

        private SyntaxToken NextToken(bool skipNewLines)
        {
            while (true)
            {
                var token = _lexer.NextToken();

                if (token.Kind == SyntaxKind.WhitespaceToken || token.Kind == SyntaxKind.InvalidToken)
                    continue;

                if (skipNewLines && token.Kind == SyntaxKind.NewLineToken)
                    continue;

                return token;
            }
        }

        private SyntaxToken Accept(bool skipNewLines)
        {
            var token = _currentToken;
            _currentToken = NextToken(skipNewLines);
            _previousToken = token;
            return token;
        }

        private SyntaxToken AcceptStatementTerminator(bool skipNewLines = false)
        {
            if (_currentToken.Kind == SyntaxKind.NewLineToken)
            {
                return Accept(skipNewLines);
            }

            if (_currentToken.Kind == SyntaxKind.EndOfInputToken)
            {
                // return end of file token but do not consume it
                return _currentToken;
            }

            _diagnostics.ReportUnexpectedToken(_currentToken.Span, _currentToken.Kind, SyntaxKind.NewLineToken);

            return new SyntaxToken(SyntaxKind.NewLineToken, _currentToken.Position, string.Empty, null);
        }

        private SyntaxToken Accept(SyntaxKind kind, bool skipNewLines = false)
        {
            if (_currentToken.Kind == kind)
                return Accept(skipNewLines);

            _diagnostics.ReportUnexpectedToken(_currentToken.Span, _currentToken.Kind, kind);

            return new SyntaxToken(kind, _currentToken.Position, string.Empty, null);
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            var statement = ParseStatement();

            var endToken = Accept(SyntaxKind.EndOfInputToken);

            return new CompilationUnitSyntax(statement, endToken);
        }

        private ExpressionSyntax ParseExpression(OperatorPrecedence precedence, bool skipNewLines)
        {
            var prefixFunction = PrefixParseFunctions.GetValueOrDefault(_currentToken.Kind);
            if (prefixFunction == null)
            {
                // no prefix function
                throw new Exception($"missing prefix function for {_currentToken.Kind}");
                return null;
            }

            skipNewLines = _currentToken.Kind != SyntaxKind.OpenBraceToken && skipNewLines;

            var left = prefixFunction(skipNewLines);

            while (precedence < CurrentPrecedence())
            {
                var infix = InfixParseFunctions.GetValueOrDefault(_currentToken.Kind);
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
            _currentToken.Kind.GetBinaryOperatorPrecedence() ?? OperatorPrecedence.Lowest;

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
            if (_currentToken.Kind == SyntaxKind.CloseParenToken)
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
            var value = _currentToken.Kind == SyntaxKind.TrueKeyword;
            return new LiteralExpressionSyntax(Accept(false), value);
        }

        private ExpressionSyntax ParseBlockExpression(bool skipNewLines)
        {
            var statements = new List<StatementSyntax>();

            var openBraceToken = Accept(SyntaxKind.OpenBraceToken, true);

            while (_currentToken.Kind != SyntaxKind.EndOfInputToken && _currentToken.Kind != SyntaxKind.CloseBraceToken)
            {
                statements.Add(ParseStatement());
            }

            var closeBraceToken = Accept(SyntaxKind.CloseBraceToken, false);

            var expr = (statements.LastOrDefault() as ExpressionStatementSyntax)?.Expression ?? new UnitExpressionSyntax(closeBraceToken, closeBraceToken);
            var stmts = statements.Take(statements.Count - 1);

            return new BlockExpressionSyntax(openBraceToken, stmts.ToImmutableArray(), expr, closeBraceToken);
        }

        private StatementSyntax ParseStatement() =>
            _currentToken.Kind switch
            {
                SyntaxKind.ValKeyword => ParseAssignmentStatement(),
                SyntaxKind.VarKeyword => ParseAssignmentStatement(),
                _ => ParseExpressionStatement(),
            };

        private StatementSyntax ParseExpressionStatement()
        {
            var expr = ParseExpression(OperatorPrecedence.Lowest, false);
            var newLineToken = AcceptStatementTerminator();
            return new ExpressionStatementSyntax(expr, newLineToken);
        }

        private StatementSyntax ParseAssignmentStatement()
        {
            var valToken = Accept(false);
            var identToken = Accept(SyntaxKind.IdentifierToken, false);
            var equalsToken = Accept(SyntaxKind.EqualsToken, false);
            var expr = ParseExpression(OperatorPrecedence.Lowest, false);
            var newLineToken = AcceptStatementTerminator();

            return new AssignmentStatementSyntax(valToken, identToken, equalsToken, expr, newLineToken);
        }
    }
}