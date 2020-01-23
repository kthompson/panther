using System;
using System.Collections.Generic;

namespace Panther.CodeAnalysis
{
    internal class Lexer
    {
        private readonly char[] _text;
        private readonly List<string> _diagnostics = new List<string>();
        private int _position;

        public Lexer(string text)
        {
            _text = text.ToCharArray();
        }

        public IEnumerable<string> Diagnostics => _diagnostics;

        private char Current => _position >= _text.Length ? '\0' : _text[_position];

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
                    _diagnostics.Add($"The number {span.AsSpan().ToString()} cannot be represented by an Int32");

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

                default:
                    _diagnostics.Add($"Error: Invalid character in input: {Current}");
                    return ReturnKindOneChar(SyntaxKind.InvalidToken);
            }
        }

        private SyntaxToken ReturnKindOneChar(SyntaxKind kind)
        {
            var token = new SyntaxToken(kind, _position, _text[_position..(_position + 1)], null);
            _position++;
            return token;
        }

        private int PeekChar()
        {
            var sourceIndex = this._position + 1;
            if (sourceIndex >= this._text.Length)
                return -1;

            return this._text[sourceIndex];
        }
    }
}