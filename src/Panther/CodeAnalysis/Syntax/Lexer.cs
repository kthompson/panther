using System;
using System.Collections.Generic;

namespace Panther.CodeAnalysis.Syntax
{
    internal class Lexer
    {
        private readonly char[] _text;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private int _position;

        public Lexer(string text)
        {
            _text = text.ToCharArray();
        }

        public IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        private char Current => Peek(_position);
        private char Lookahead => Peek(_position + 1);

        private char Peek(int position) => position >= _text.Length ? '\0' : _text[position];

        private void Next()
        {
            _position++;
        }

        public SyntaxToken NextToken()
        {
            if (_position >= _text.Length)
                return new SyntaxToken(SyntaxKind.EndOfInputToken, _position, Span<char>.Empty, null);

            if (char.IsDigit(Current))
            {
                var start = _position;

                while (char.IsDigit(Current))
                {
                    Next();
                }

                var span = _text[start.._position];
                if (!int.TryParse(span, out var value))
                    _diagnostics.ReportInvalidNumber(new TextSpan(start, _position - start), span.AsSpan().ToString(),
                        typeof(int));

                return new SyntaxToken(SyntaxKind.NumberToken, start, span, value);
            }

            if (char.IsWhiteSpace(Current))
            {
                var start = _position;

                while (char.IsWhiteSpace(Current))
                {
                    Next();
                }

                var span = _text[start.._position];

                return new SyntaxToken(SyntaxKind.WhitespaceToken, start, span, null);
            }

            if (char.IsLetter(Current))
            {
                var start = _position;

                while (char.IsLetter(Current))
                {
                    Next();
                }

                var span = _text[start.._position];

                var kind = SyntaxFacts.GetKeywordKind(span);

                return new SyntaxToken(kind, start, span, null);
            }

            switch (Current)
            {
                case '+':
                    return ReturnKindOneChar(SyntaxKind.PlusToken);

                case '-':
                    return ReturnKindOneChar(SyntaxKind.MinusToken);

                case '/':
                    return ReturnKindOneChar(SyntaxKind.SlashToken);

                case '*':
                    return ReturnKindOneChar(SyntaxKind.StarToken);

                case '(':
                    return ReturnKindOneChar(SyntaxKind.OpenParenToken);

                case ')':
                    return ReturnKindOneChar(SyntaxKind.CloseParenToken);

                case '!':
                    return Lookahead == '='
                        ? ReturnKindTwoChar(SyntaxKind.BangEqualsToken)
                        : ReturnKindOneChar(SyntaxKind.BangToken);

                case '&' when Lookahead == '&':
                    return ReturnKindTwoChar(SyntaxKind.AmpersandAmpersandToken);

                case '|' when Lookahead == '|':
                    return ReturnKindTwoChar(SyntaxKind.PipePipeToken);

                case '=':
                    return Lookahead == '='
                        ? ReturnKindTwoChar(SyntaxKind.EqualsEqualsToken)
                        : ReturnKindOneChar(SyntaxKind.EqualsToken);

                default:
                    _diagnostics.ReportBadCharacter(_position, Current);
                    return ReturnKindOneChar(SyntaxKind.InvalidToken);
            }
        }

        private SyntaxToken ReturnKindTwoChar(SyntaxKind kind)
        {
            var token = new SyntaxToken(kind, _position, _text[_position..(_position + 2)], null);
            _position += 2;
            return token;
        }

        private SyntaxToken ReturnKindOneChar(SyntaxKind kind)
        {
            var token = new SyntaxToken(kind, _position, _text[_position..(_position + 1)], null);
            _position++;
            return token;
        }
    }
}