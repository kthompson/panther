using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Syntax;

internal class Lexer
{
    private readonly SyntaxTree _syntaxTree;
    private readonly SourceFile _file;
    private readonly DiagnosticBag _diagnostics = new();
    private int _position;
    private readonly Dictionary<char, Func<(SyntaxKind kind, int start, string text, object? value)>> _lexFunctions = new();

    public Lexer(SyntaxTree syntaxTree)
    {
        _syntaxTree = syntaxTree;
        _file = syntaxTree.File;
        InitializeLexFunctions();
    }

    private void InitializeLexFunctions()
    {
        _lexFunctions['\0'] = ReturnEndOfInput;
        _lexFunctions['!'] = ReturnBangToken;
        _lexFunctions['"'] = ParseStringToken;
        _lexFunctions['&'] = ReturnAmpersandToken;
        _lexFunctions['('] = ReturnOpenParenToken;
        _lexFunctions[')'] = ReturnCloseParenToken;
        _lexFunctions['*'] = ReturnStarToken;
        _lexFunctions['+'] = ReturnPlusToken;
        _lexFunctions[','] = ReturnCommaToken;
        _lexFunctions['-'] = ReturnDashToken;
        _lexFunctions['.'] = ReturnDotToken;
        _lexFunctions['/'] = ReturnSlashToken;
        _lexFunctions[':'] = ReturnColonToken;
        _lexFunctions['<'] = ReturnLessThanToken;
        _lexFunctions['='] = ReturnEqualsToken;
        _lexFunctions['>'] = ReturnGreaterThanToken;
        _lexFunctions['^'] = ReturnCaretToken;
        _lexFunctions['{'] = ReturnOpenBraceToken;
        _lexFunctions['|'] = ReturnPipeToken;
        _lexFunctions['}'] = ReturnCloseBraceToken;
        _lexFunctions['~'] = ReturnTildeToken;

        for (var i = '0'; i <= '9'; i++)
            _lexFunctions[i] = ParseNumber;
    }

    public IEnumerable<Diagnostic> Diagnostics => _diagnostics;

    private char Current => Peek(_position);
    private char Lookahead => Peek(_position + 1);

    private char Peek(int position) => position >= _file.Length ? '\0' : _file[position];

    private void Next()
    {
        _position++;
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

                var span = _file[start..end];

                trivia.Add(new SyntaxTrivia(_syntaxTree, SyntaxKind.EndOfLineTrivia, span, start));
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

                var span = _file[start..end];
                trivia.Add(new SyntaxTrivia(_syntaxTree, SyntaxKind.WhitespaceTrivia, span, start));
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

            if (IsInvalidTokenTrivia())
            {
                trivia.Add(ParseInvalidTokenTrivia());
            }
            // non-trivia
            break;
        }

        return trivia.ToImmutable();
    }

    private bool IsInvalidTokenTrivia() => !_lexFunctions.ContainsKey(Current) && !IsIdentCharacter(Current, true);

    private SyntaxTrivia ParseInvalidTokenTrivia()
    {
        var (kind, start, text, _) = ReturnInvalidTokenTrivia();

        return new SyntaxTrivia(_syntaxTree, kind, text, start);
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
                _diagnostics.ReportUnterminatedBlockComment(new TextLocation(_file, new TextSpan(start, _position)));
                return new SyntaxTrivia(_syntaxTree, SyntaxKind.BlockCommentTrivia, _file[start.._position], start);
            }

            if (Current == '*' && Lookahead == '/')
            {
                Next(); // '*'
                Next(); // '/'
                return new SyntaxTrivia(_syntaxTree, SyntaxKind.BlockCommentTrivia, _file[start.._position], start);
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

        var (kind, start, text, value) = ParseToken();

        var trailing = ParseTrivia(false);

        return new SyntaxToken(_syntaxTree, kind, start, text, value, leading, trailing);
    }

    private (SyntaxKind kind, int start, string text, object? value) ParseToken()
    {
        if (_lexFunctions.TryGetValue(Current, out var function))
        {
            return function();
        }

        if (IsIdentCharacter(Current, true))
            return ParseIdentOrKeyword();

        return ReturnInvalidTokenTrivia();
    }

    private static bool IsIdentCharacter(char current, bool leading) =>
        leading
            ? char.IsLetter(current) || current == '_'
            : char.IsLetterOrDigit(current) || current == '_';

    private (SyntaxKind kind, int start, string text, object? value) ReturnInvalidTokenTrivia()
    {
        _diagnostics.ReportBadCharacter(new TextLocation(_file, new TextSpan(_position, 1)), Current);
        return ReturnKindOneChar(SyntaxKind.InvalidTokenTrivia);
    }

    private (SyntaxKind kind, int start, string text, object? value) ReturnEqualsToken() =>
        Lookahead == '='
            ? ReturnKindTwoChar(SyntaxKind.EqualsEqualsToken)
            : ReturnKindOneChar(SyntaxKind.EqualsToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnPipeToken() =>
        Lookahead == '|'
            ? ReturnKindTwoChar(SyntaxKind.PipePipeToken)
            : ReturnKindOneChar(SyntaxKind.PipeToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnAmpersandToken() =>
        Lookahead == '&'
            ? ReturnKindTwoChar(SyntaxKind.AmpersandAmpersandToken)
            : ReturnKindOneChar(SyntaxKind.AmpersandToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnBangToken() =>
        Lookahead == '='
            ? ReturnKindTwoChar(SyntaxKind.BangEqualsToken)
            : ReturnKindOneChar(SyntaxKind.BangToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnLessThanToken() =>
        Lookahead switch
        {
            '=' => ReturnKindTwoChar(SyntaxKind.LessThanEqualsToken),
            '-' => ReturnKindTwoChar(SyntaxKind.LessThanDashToken),
            _ => ReturnKindOneChar(SyntaxKind.LessThanToken)
        };

    private (SyntaxKind kind, int start, string text, object? value) ReturnGreaterThanToken() =>
        Lookahead == '='
            ? ReturnKindTwoChar(SyntaxKind.GreaterThanEqualsToken)
            : ReturnKindOneChar(SyntaxKind.GreaterThanToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnColonToken() => ReturnKindOneChar(SyntaxKind.ColonToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnCommaToken() => ReturnKindOneChar(SyntaxKind.CommaToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnTildeToken() => ReturnKindOneChar(SyntaxKind.TildeToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnCaretToken() => ReturnKindOneChar(SyntaxKind.CaretToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnCloseBraceToken() => ReturnKindOneChar(SyntaxKind.CloseBraceToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnOpenBraceToken() => ReturnKindOneChar(SyntaxKind.OpenBraceToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnCloseParenToken() => ReturnKindOneChar(SyntaxKind.CloseParenToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnOpenParenToken() => ReturnKindOneChar(SyntaxKind.OpenParenToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnStarToken() => ReturnKindOneChar(SyntaxKind.StarToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnSlashToken() => ReturnKindOneChar(SyntaxKind.SlashToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnDashToken() => ReturnKindOneChar(SyntaxKind.DashToken);
    private (SyntaxKind kind, int start, string text, object? value) ReturnDotToken() => ReturnKindOneChar(SyntaxKind.DotToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnPlusToken() => ReturnKindOneChar(SyntaxKind.PlusToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnEndOfInput() => (SyntaxKind.EndOfInputToken, _position, string.Empty, null);

    private (SyntaxKind kind, int start, string text, object? value) ParseIdentOrKeyword()
    {
        var start = _position;
        Next(); // skip first letter
        while (IsIdentCharacter(Current, false))
        {
            Next();
        }

        var span = _file[start.._position];

        var kind = SyntaxFacts.GetKeywordKind(span);

        return (kind, start, span, null);
    }

    private (SyntaxKind kind, int start, string text, object value) ParseNumber()
    {
        var start = _position;
        ParsePredicate(char.IsDigit);

        var span = _file[start.._position];

        if (!int.TryParse(span, out var value))
            _diagnostics.ReportInvalidNumber(
                new TextLocation(_file, new TextSpan(start, _position - start)),
                span.AsSpan().ToString(),
                Type.Int);

        return (SyntaxKind.NumberToken, start, span, value);
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

        return new SyntaxTrivia(_syntaxTree, SyntaxKind.LineCommentTrivia, _file[start.._position], start);
    }

    private (SyntaxKind kind, int start, string text, object value) ParseStringToken()
    {
        int start = _position;
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
                    _diagnostics.ReportUnterminatedString(new TextLocation(_file, new TextSpan(start, 1)));
                    break;

                default:
                    sb.Append(Current);
                    Next();
                    continue;
            }

            break;
        }

        var span = _file[start.._position];

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
                    new TextLocation(_file, new TextSpan(escapeStart, _position)), Current);
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
                _diagnostics.ReportInvalidEscapeSequence(new TextLocation(_file, new TextSpan(escapeStart, _position)), Current);
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
        var text = _file[_position..(_position + 2)];
        _position += 2;
        return (kind, start, text, null);
    }

    private (SyntaxKind kind, int start, string text, object? value) ReturnKindOneChar(SyntaxKind kind)
    {
        var start = _position;
        var text = _file[_position..(_position + 1)];
        _position++;
        return (kind, start, text, null);
    }
}