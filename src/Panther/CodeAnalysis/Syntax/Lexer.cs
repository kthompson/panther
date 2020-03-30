using System;
using System.Collections.Generic;
using System.Text;
using Panther.CodeAnalysis.Symbols;
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

            switch (Current)
            {
                case '\0':
                    return new SyntaxToken(SyntaxKind.EndOfInputToken, _position, string.Empty, null);

                case '+':
                    return ReturnKindOneChar(SyntaxKind.PlusToken);

                case '-':
                    return ReturnKindOneChar(SyntaxKind.DashToken);

                case '/':
                    return ReturnKindOneChar(SyntaxKind.SlashToken);

                case '*':
                    return ReturnKindOneChar(SyntaxKind.StarToken);

                case '(':
                    return ReturnKindOneChar(SyntaxKind.OpenParenToken);

                case ')':
                    return ReturnKindOneChar(SyntaxKind.CloseParenToken);

                case '{':
                    return ReturnKindOneChar(SyntaxKind.OpenBraceToken);

                case '}':
                    return ReturnKindOneChar(SyntaxKind.CloseBraceToken);

                case '^':
                    return ReturnKindOneChar(SyntaxKind.CaretToken);

                case '~':
                    return ReturnKindOneChar(SyntaxKind.TildeToken);
                
                case ',':
                    return ReturnKindOneChar(SyntaxKind.CommaToken);

                case '>':
                    return Lookahead == '='
                        ? ReturnKindTwoChar(SyntaxKind.GreaterThanEqualsToken)
                        : ReturnKindOneChar(SyntaxKind.GreaterThanToken);

                case '<':
                    return Lookahead == '='
                        ? ReturnKindTwoChar(SyntaxKind.LessThanEqualsToken)
                        : Lookahead == '-'
                            ? ReturnKindTwoChar(SyntaxKind.LessThanDashToken)
                            : ReturnKindOneChar(SyntaxKind.LessThanToken);

                case '!':
                    return Lookahead == '='
                        ? ReturnKindTwoChar(SyntaxKind.BangEqualsToken)
                        : ReturnKindOneChar(SyntaxKind.BangToken);

                case '&':
                    return Lookahead == '&'
                        ? ReturnKindTwoChar(SyntaxKind.AmpersandAmpersandToken)
                        : ReturnKindOneChar(SyntaxKind.AmpersandToken);

                case '|':
                    return Lookahead == '|'
                        ? ReturnKindTwoChar(SyntaxKind.PipePipeToken)
                        : ReturnKindOneChar(SyntaxKind.PipeToken);

                case '=':
                    return Lookahead == '='
                        ? ReturnKindTwoChar(SyntaxKind.EqualsEqualsToken)
                        : ReturnKindOneChar(SyntaxKind.EqualsToken);

                case '"':
                    return ParseStringToken(start);

                default:

                    bool IsNonNewLineWhiteSpace(char c1) => c1 != '\n' && c1 != '\r' && char.IsWhiteSpace(c1);

                    bool IsNewLine(char c) => c == '\n' || c == '\r';

                    if (IfWhile(IsNewLine))
                    {
                        var span = Text[start.._position];

                        return new SyntaxToken(SyntaxKind.NewLineToken, start, span, null);
                    }
                    if (IfWhile(char.IsDigit))
                    {
                        var span = Text[start.._position];

                        if (!int.TryParse(span, out var value))
                            _diagnostics.ReportInvalidNumber(new TextSpan(start, _position - start), span.AsSpan().ToString(),
                                TypeSymbol.Int);

                        return new SyntaxToken(SyntaxKind.NumberToken, start, span, value);
                    }

                    if (IfWhile(IsNonNewLineWhiteSpace))
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

                    _diagnostics.ReportBadCharacter(_position, Current);
                    return ReturnKindOneChar(SyntaxKind.InvalidToken);
            }
        }

        private SyntaxToken ParseStringToken(int start)
        {
            Next(); // start "
            var sb = new StringBuilder();
            while (true)
            {
                switch (Current)
                {
                    case '"':
                        Next(); // end "
                        break;

                    case '\\': // escape sequence
                        var escapeSequence = ParseEscapeSequence();
                        if (escapeSequence != null)
                            sb.Append(escapeSequence);

                        continue;
                    case '\n':
                    case '\r':
                    case '\0':
                        _diagnostics.ReportUnterminatedString(new TextSpan(start, 1));
                        break;

                    default:
                        sb.Append(Current);
                        Next();
                        continue;
                }

                break;
            }

            var span = Text[start.._position];

            return new SyntaxToken(SyntaxKind.StringToken, start, span, sb.ToString());
        }

        private string? ParseEscapeSequence()
        {
            var escapeStart = _position;
            Next(); // accept \
            switch (Current)
            {
                case 'r':
                    Next();
                    return "\r";

                case 'n':
                    Next();
                    return "\n";

                case 't':
                    Next();
                    return "\t";

                case '\\':
                    Next();
                    return "\\";

                case '"':
                    Next();
                    return "\"";

                case 'u':
                    Next(); //u
                    return ParseUtfEscapeSequence(4, escapeStart);

                case 'U':
                    Next(); //U
                    return ParseUtfEscapeSequence(8, escapeStart);

                default:
                    _diagnostics.ReportInvalidEscapeSequence(escapeStart, _position, Current);
                    return null;
            }
        }

        private string? ParseUtfEscapeSequence(int digits, int escapeStart)
        {
            var value = 0;
            for (var i = 0; i < digits; i++)
            {
                if (!HexValue(out var hexValue))
                {
                    _diagnostics.ReportInvalidEscapeSequence(escapeStart, _position, Current);
                    return null;
                }

                value += hexValue << 4 * (digits - 1 - i);
                Next();
            }

            return ((char)value).ToString();
        }

        private bool HexValue(out int value)
        {
            try
            {
                value = int.Parse(Current.ToString(), System.Globalization.NumberStyles.HexNumber);
                return true;
            }
            catch
            {
                value = 0;
                return false;
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