using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;

namespace Panther.CodeAnalysis.Syntax
{
    internal class Lexer
    {
        private readonly SyntaxTree _syntaxTree;
        private readonly SourceText _text;
        private readonly DiagnosticBag _diagnostics = new DiagnosticBag();
        private int _position;

        public Lexer(SyntaxTree syntaxTree)
        {
            _syntaxTree = syntaxTree;
            _text = syntaxTree.Text;
        }

        public IEnumerable<Diagnostic> Diagnostics => _diagnostics;

        private char Current => Peek(_position);
        private char Lookahead => Peek(_position + 1);

        private char Peek(int position) => position >= _text.Length ? '\0' : _text[position];

        private void Next()
        {
            _position++;
        }

        private bool IfWhile(Func<char, bool> predicate)
        {
            if (!predicate(Current))
                return false;

            ParsePredicate(predicate);

            return true;
        }

        private (int start, int end) ParsePredicate(Func<char, bool> predicate)
        {
            var start = _position;
            while (predicate(Current))
            {
                Next();
            }

            var end = _position;
            return (start, end);
        }

        private ImmutableArray<SyntaxTrivia> ParseTrivia(bool leadingTrivia)
        {
            var trivia = ImmutableArray.CreateBuilder<SyntaxTrivia>();
            while (true)
            {
                if (IsEndOfLine(Current))
                {
                    var (start, end) = ParsePredicate(IsEndOfLine);

                    var span = _text[start..end];

                    trivia.Add(new SyntaxTrivia(_syntaxTree, SyntaxKind.EndOfLineTrivia, span));
                    if (!leadingTrivia)
                    {
                        // trailing trivia should always terminate at the end of a line
                        break;
                    }
                    // leading trivia will terminate once we find out first non-trivia token
                    continue;
                }

                if (IsNonNewLineWhiteSpace(Current))
                {
                    var (start, end) = ParsePredicate(IsNonNewLineWhiteSpace);

                    var span = _text[start..end];
                    trivia.Add(new SyntaxTrivia(_syntaxTree, SyntaxKind.WhitespaceTrivia, span));
                    continue;
                }

                if (Current == '/' && Lookahead == '/')
                {
                    trivia.Add(ParseLineComment());
                    continue;
                }

                if (Current == '/' && Lookahead == '*')
                {
                    trivia.Add(ParseBlockComment());
                    continue;
                }

                // non-trivia
                break;
            }

            return trivia.ToImmutable();
        }

        private SyntaxTrivia ParseBlockComment()
        {
            var start = _position;
            Next(); // '/'
            Next(); // '*'

            while (true)
            {
                if (Current == '\0')
                {
                    _diagnostics.ReportUnterminatedBlockComment(new TextLocation(_text, new TextSpan(start, _position)));
                    return new SyntaxTrivia(_syntaxTree, SyntaxKind.BlockCommentTrivia, _text[start.._position]);
                }

                if (Current == '*' && Lookahead == '/')
                {
                    Next(); // '*'
                    Next(); // '/'
                    return new SyntaxTrivia(_syntaxTree, SyntaxKind.BlockCommentTrivia, _text[start.._position]);
                }

                Next();
            }
        }

        bool IsEndOfLine(char c) => c == '\n' || c == '\r';

        bool IsNonNewLineWhiteSpace(char c1) => !IsEndOfLine(c1) && char.IsWhiteSpace(c1);

        public SyntaxToken NextToken()
        {
            // leading trivia only occurs for the first token on a line
            //                or before in the case of trivia only lines
            var leading = ParseTrivia(true);
            // Trailing trivia is anything after the first token

            var ( kind, start, text, value) = ParseToken();

            var trailing = ParseTrivia(false);

            return new SyntaxToken(_syntaxTree, kind, start, text, value, leading, trailing);
        }

        private (SyntaxKind kind, int start, string text, object? value) ParseToken()
        {
            var start = _position;

            switch (Current)
            {
                case '\0':
                    return (SyntaxKind.EndOfInputToken, _position, string.Empty, null);

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

                case ':':
                    return ReturnKindOneChar(SyntaxKind.ColonToken);

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

                    if (IfWhile(char.IsDigit))
                    {
                        var span = _text[start.._position];

                        if (!int.TryParse(span, out var value))
                            _diagnostics.ReportInvalidNumber(
                                new TextLocation(_text, new TextSpan(start, _position - start)),
                                span.AsSpan().ToString(),
                                TypeSymbol.Int);

                        return (SyntaxKind.NumberToken, start, span, value);
                    }

                    if (IfWhile(char.IsLetter))
                    {
                        var span = _text[start.._position];

                        var kind = SyntaxFacts.GetKeywordKind(span);

                        return (kind, start, span, null);
                    }

                    _diagnostics.ReportBadCharacter(
                        new TextLocation(_text, new TextSpan(_position, 1)), Current);
                    return ReturnKindOneChar(SyntaxKind.InvalidToken);
            }
        }

        private SyntaxTrivia ParseLineComment()
        {
            var start = _position;

            Next(); // '/'
            Next(); // '/'
            while (Current != '\r' && Current != '\n' && Current != '\0')
            {
                Next();
            }

            return new SyntaxTrivia(_syntaxTree, SyntaxKind.LineCommentTrivia, _text[start.._position]);
        }

        private (SyntaxKind kind, int start, string text, object value) ParseStringToken(int start)
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
                        _diagnostics.ReportUnterminatedString(new TextLocation(_text, new TextSpan(start, 1)));
                        break;

                    default:
                        sb.Append(Current);
                        Next();
                        continue;
                }

                break;
            }

            var span = _text[start.._position];

            return (SyntaxKind.StringToken, start, span, sb.ToString());
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
                    _diagnostics.ReportInvalidEscapeSequence(
                        new TextLocation(_text, new TextSpan(escapeStart, _position)), Current);
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
                    _diagnostics.ReportInvalidEscapeSequence(new TextLocation(_text, new TextSpan(escapeStart, _position)), Current);
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

        private (SyntaxKind kind, int start, string text, object? value) ReturnKindTwoChar(SyntaxKind kind)
        {
            var start = _position;
            var text = _text[_position..(_position + 2)];
            _position += 2;
            return (kind, start, text, null);
        }

        private (SyntaxKind kind, int start, string text, object? value) ReturnKindOneChar(SyntaxKind kind)
        {
            var start = _position;
            var text = _text[_position..(_position + 1)];
            _position++;
            return (kind, start, text, null);
        }
    }
}