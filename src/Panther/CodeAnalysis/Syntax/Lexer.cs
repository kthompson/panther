using System;
using System.Collections.Generic;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    internal class Lexer
    {
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private int _position;

        public Lexer(SourceText text)
        {
            Text = text;
        }

        public SourceText Text { get; }

        public IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        private char Current => Peek(_position);
        private char Lookahead => Peek(_position + 1);

        private char Peek(int position) => position >= Text.Length ? '\0' : Text[position];

        private void Next()
        {
            _position++;
        }

        private bool IfWhile(Func<char, bool> predicate)
        {
            if (!predicate(Current))
                return false;

            while (predicate(Current))
            {
                Next();
            }

            return true;
        }

        public SyntaxToken NextToken()
        {
            var start = _position;

            if (IfWhile(char.IsDigit))
            {
                var span = Text[start.._position];

                if (!int.TryParse(span, out var value))
                    _diagnostics.ReportInvalidNumber(new TextSpan(start, _position - start), span.AsSpan().ToString(),
                        typeof(int));

                return new SyntaxToken(SyntaxKind.NumberToken, start, span, value);
            }

            if (IfWhile(char.IsWhiteSpace))
            {
                var span = Text[start.._position];

                return new SyntaxToken(SyntaxKind.WhitespaceToken, start, span, null);
            }

            if (IfWhile(char.IsLetter))
            {
                var span = Text[start.._position];

                var kind = SyntaxFacts.GetKeywordKind(span);

                return new SyntaxToken(kind, start, span, null);
            }

            switch (Current)
            {
                case '\0':
                    return new SyntaxToken(SyntaxKind.EndOfInputToken, _position, string.Empty, null);

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
            var token = new SyntaxToken(kind, _position, Text[_position..(_position + 2)], null);
            _position += 2;
            return token;
        }

        private SyntaxToken ReturnKindOneChar(SyntaxKind kind)
        {
            var token = new SyntaxToken(kind, _position, Text[_position..(_position + 1)], null);
            _position++;
            return token;
        }
    }
}