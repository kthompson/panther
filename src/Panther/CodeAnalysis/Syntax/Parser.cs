using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    internal class Parser
    {
        private readonly Lexer _lexer;
        private SyntaxToken _currentToken;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();

        public DiagnosticBag Diagnostics => new DiagnosticBag().AddRange(_lexer.Diagnostics).AddRange(_diagnostics);

        public Parser(SourceText text)
            : this(new Lexer(text))
        {
        }

        public Parser(Lexer lexer)
        {
            _lexer = lexer;
            // init first token
            Accept();
        }

        private SyntaxToken NextToken()
        {
            while (true)
            {
                var token = _lexer.NextToken();

                if (token.Kind == SyntaxKind.WhitespaceToken || token.Kind == SyntaxKind.InvalidToken)
                    continue;

                return token;
            }
        }

        private SyntaxToken Accept()
        {
            var token = _currentToken;
            _currentToken = NextToken();
            return token;
        }

        private SyntaxToken Accept(SyntaxKind kind)
        {
            if (_currentToken.Kind == kind)
                return Accept();

            _diagnostics.ReportUnexpectedToken(_currentToken.Span, _currentToken.Kind, kind);

            return new SyntaxToken(kind, _currentToken.Position, string.Empty, null);
        }

        public CompilationUnitSyntax ParseCompilationUnit()
        {
            var statement = ParseStatement();

            var endToken = Accept(SyntaxKind.EndOfInputToken);

            return new CompilationUnitSyntax(statement, endToken);
        }

        private ExpressionSyntax ParseExpression(int parentPrecedence = 0)
        {
            ExpressionSyntax left;
            var unaryOperatorPrecedence = _currentToken.Kind.GetUnaryOperatorPrecedence();

            if (unaryOperatorPrecedence != 0 && unaryOperatorPrecedence >= parentPrecedence)
            {
                var operatorToken = Accept();
                var operand = ParseExpression(unaryOperatorPrecedence);

                left = new UnaryExpressionSyntax(operatorToken, operand);
            }
            else
            {
                left = ParsePrimaryExpression();
            }

            while (true)
            {
                var precedence = _currentToken.Kind.GetBinaryOperatorPrecedence();
                if (precedence == 0 || precedence <= parentPrecedence)
                    break;

                var operatorToken = Accept();
                var right = ParseExpression(precedence);
                left = new BinaryExpressionSyntax(left, operatorToken, right);
            }

            return left;
        }

        private ExpressionSyntax ParsePrimaryExpression()
        {
            switch (_currentToken.Kind)
            {
                case SyntaxKind.OpenParenToken:
                    {
                        var open = Accept();
                        var expr = ParseExpression();
                        var close = Accept(SyntaxKind.CloseParenToken);

                        return new GroupExpressionSyntax(open, expr, close);
                    }

                case SyntaxKind.OpenBraceToken:

                    {
                        return ParseBlockExpression();
                    }

                case SyntaxKind.TrueKeyword:
                case SyntaxKind.FalseKeyword:

                    var value = _currentToken.Kind == SyntaxKind.TrueKeyword;

                    return new LiteralExpressionSyntax(Accept(), value);

                case SyntaxKind.IdentifierToken:
                    var token = Accept();

                    return new NameExpressionSyntax(token);

                default:
                    var numberToken = Accept(SyntaxKind.NumberToken);
                    return new LiteralExpressionSyntax(numberToken);
            }
        }

        private ExpressionSyntax ParseBlockExpression()
        {
            var statements = new List<StatementSyntax>();

            var openBraceToken = Accept(SyntaxKind.OpenBraceToken);

            while (_currentToken.Kind != SyntaxKind.EndOfInputToken && _currentToken.Kind != SyntaxKind.CloseBraceToken)
            {
                statements.Add(ParseStatement());
            }

            var closeBraceToken = Accept(SyntaxKind.CloseBraceToken);

            var expr = (statements.LastOrDefault() as ExpressionStatementSyntax)?.Expression;

            var stmts = expr != null ? statements.Take(statements.Count - 1) : statements;

            return new BlockExpressionSyntax(openBraceToken, stmts.ToImmutableArray(), expr, closeBraceToken);
        }

        private StatementSyntax ParseStatement() =>
            _currentToken.Kind switch
            {
                SyntaxKind.ValKeyword => ParseAssignmentStatement(),
                _ => ParseExpressionStatement(),
            };

        private StatementSyntax ParseExpressionStatement()
        {
            var expr = ParseExpression();
            return new ExpressionStatementSyntax(expr);
        }

        private StatementSyntax ParseAssignmentStatement()
        {
            var valToken = Accept();
            var identToken = Accept(SyntaxKind.IdentifierToken);
            var equalsToken = Accept(SyntaxKind.EqualsToken);
            var expr = ParseExpression();

            return new AssignmentStatementSyntax(valToken, identToken, equalsToken, expr);
        }
    }
}