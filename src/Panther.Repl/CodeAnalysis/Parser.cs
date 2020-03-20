﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Panther.CodeAnalysis
{
    internal class Parser
    {
        private readonly Lexer _lexer;
        private SyntaxToken _currentToken;
        private readonly List<string> _diagnostics = new List<string>();

        public Parser(string text)
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

            _diagnostics.Add($"Error: Unexpected token {_currentToken.Kind}, expected {kind}");

            return new SyntaxToken(kind, _currentToken.Position, Span<char>.Empty, null);
        }

        public SyntaxTree Parse()
        {
            var expression = ParseExpression();

            var endToken = Accept(SyntaxKind.EndOfInputToken);

            return new SyntaxTree(_lexer.Diagnostics.Concat(_diagnostics), expression, endToken);
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
            if (_currentToken.Kind == SyntaxKind.OpenParenToken)
            {
                var open = Accept();
                var expr = ParseExpression();
                var close = Accept(SyntaxKind.CloseParenToken);

                return new GroupExpressionSyntax(open, expr, close);
            }

            var numberToken = Accept(SyntaxKind.NumberToken);
            return new LiteralExpressionSyntax(numberToken);
        }
    }
}