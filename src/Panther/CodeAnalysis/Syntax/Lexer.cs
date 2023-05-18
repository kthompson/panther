using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Text;
using Type = Panther.CodeAnalysis.Symbols.Type;

namespace Panther.CodeAnalysis.Syntax;

internal class Lexer
{
    private const char EndOfInputCharacter = '\u0003';
    private readonly SourceFile _file;
    private readonly Func<string, SyntaxKind> _getKeyword;
    private readonly bool _dotIsIdentToken;
    private readonly DiagnosticBag _diagnostics = new();
    private int _position;
    private readonly Dictionary<
        char,
        Func<(SyntaxKind kind, int start, string text, object? value)>
    > _lexFunctions = new();

    public Lexer(SourceFile sourceFile, Func<string, SyntaxKind> getKeyword, bool dotIsIdentToken)
    {
        _file = sourceFile;
        _getKeyword = getKeyword;
        _dotIsIdentToken = dotIsIdentToken;
        InitializeLexFunctions();
    }

    private void InitializeLexFunctions()
    {
        _lexFunctions[EndOfInputCharacter] = ReturnEndOfInput;
        _lexFunctions['!'] = ReturnBangToken;
        _lexFunctions['"'] = ParseStringToken;
        _lexFunctions['\''] = ParseCharToken;
        _lexFunctions['&'] = ReturnAmpersandToken;
        _lexFunctions['('] = ReturnOpenParenToken;
        _lexFunctions[')'] = ReturnCloseParenToken;
        _lexFunctions['*'] = ReturnStarToken;
        _lexFunctions['+'] = ReturnPlusToken;
        _lexFunctions[','] = ReturnCommaToken;
        _lexFunctions['-'] = ReturnDashToken;
        if (!_dotIsIdentToken)
            _lexFunctions['.'] = ReturnDotToken;
        _lexFunctions['/'] = ReturnSlashToken;
        _lexFunctions[':'] = ReturnColonToken;
        _lexFunctions['<'] = ReturnLessThanToken;
        _lexFunctions['='] = ReturnEqualsToken;
        _lexFunctions['>'] = ReturnGreaterThanToken;
        _lexFunctions['['] = ReturnOpenBracketToken;
        _lexFunctions[']'] = ReturnCloseBracketToken;
        _lexFunctions['^'] = ReturnCaretToken;
        _lexFunctions['{'] = ReturnOpenBraceToken;
        _lexFunctions['|'] = ReturnPipeToken;
        _lexFunctions['}'] = ReturnCloseBraceToken;
        _lexFunctions['~'] = ReturnTildeToken;

        for (var i = '0'; i <= '9'; i++)
            _lexFunctions[i] = ParseNumber;
    }

    public IEnumerable<Diagnostic> Diagnostics => _diagnostics;

    private char? Current => Peek(_position);
    private char? Lookahead => Peek(_position + 1);

    private char? Peek(int position) => position >= _file.Length ? null : _file[position];

    private void Next()
    {
        _position++;
    }

    private (int start, int end) ParsePredicate(Func<char, bool> predicate)
    {
        var start = _position;
        while (Current != null && predicate(Current.Value))
        {
            Next();
        }

        var end = _position;
        return (start, end);
    }

    private ImmutableArray<SyntaxTrivia> ParseTrivia(bool leadingTrivia)
    {
        var trivia = ImmutableArray.CreateBuilder<SyntaxTrivia>();
        while (Current != null)
        {
            if (IsEndOfLine(Current.Value))
            {
                var (start, end) = ParsePredicate(IsEndOfLine);

                var span = _file[start..end];

                trivia.Add(new SyntaxTrivia(_file, SyntaxKind.EndOfLineTrivia, span, start));
                if (!leadingTrivia)
                {
                    // trailing trivia should always terminate at the end of a line
                    break;
                }
                // leading trivia will terminate once we find out first non-trivia token
                continue;
            }

            if (Current != null && IsNonNewLineWhiteSpace(Current.Value))
            {
                var (start, end) = ParsePredicate(IsNonNewLineWhiteSpace);

                var span = _file[start..end];
                trivia.Add(new SyntaxTrivia(_file, SyntaxKind.WhitespaceTrivia, span, start));
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

    private bool IsInvalidTokenTrivia() =>
        Current != null
        && !_lexFunctions.ContainsKey(Current.Value)
        && !IsIdentCharacter(Current.Value, true);

    private SyntaxTrivia ParseInvalidTokenTrivia()
    {
        var (kind, start, text, _) = ReturnInvalidTokenTrivia();

        return new SyntaxTrivia(_file, kind, text, start);
    }

    private SyntaxTrivia ParseBlockComment()
    {
        var start = _position;
        Next(); // '/'
        Next(); // '*'

        while (true)
        {
            if (Current == EndOfInputCharacter)
            {
                _diagnostics.ReportUnterminatedBlockComment(
                    new TextLocation(_file, new TextSpan(start, _position - start))
                );
                return new SyntaxTrivia(
                    _file,
                    SyntaxKind.BlockCommentTrivia,
                    _file[start.._position],
                    start
                );
            }

            if (Current == '*' && Lookahead == '/')
            {
                Next(); // '*'
                Next(); // '/'
                return new SyntaxTrivia(
                    _file,
                    SyntaxKind.BlockCommentTrivia,
                    _file[start.._position],
                    start
                );
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

        return new SyntaxToken(_file, kind, start, text, value, leading, trailing);
    }

    private (SyntaxKind kind, int start, string text, object? value) ParseToken()
    {
        if (Current == null)
        {
            return ReturnEndOfInput();
        }

        if (_lexFunctions.TryGetValue(Current.Value, out var function))
        {
            return function();
        }

        if (IsIdentCharacter(Current.Value, true))
            return ParseIdentOrKeyword();

        return ReturnInvalidTokenTrivia();
    }

    private bool IsIdentCharacter(char current, bool leading)
    {
        if (char.IsLetter(current) || current == '_')
            return true;

        if (!leading)
            return char.IsDigit(current);

        return _dotIsIdentToken && current == '.';
    }

    private (SyntaxKind kind, int start, string text, object? value) ReturnInvalidTokenTrivia()
    {
        _diagnostics.ReportBadCharacter(
            new TextLocation(_file, new TextSpan(_position, 1)),
            Current!.Value
        );
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

    private (SyntaxKind kind, int start, string text, object? value) ReturnColonToken() =>
        ReturnKindOneChar(SyntaxKind.ColonToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnCommaToken() =>
        ReturnKindOneChar(SyntaxKind.CommaToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnTildeToken() =>
        ReturnKindOneChar(SyntaxKind.TildeToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnCaretToken() =>
        ReturnKindOneChar(SyntaxKind.CaretToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnCloseBraceToken() =>
        ReturnKindOneChar(SyntaxKind.CloseBraceToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnOpenBracketToken() =>
        ReturnKindOneChar(SyntaxKind.OpenBracketToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnCloseBracketToken() =>
        ReturnKindOneChar(SyntaxKind.CloseBracketToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnOpenBraceToken() =>
        ReturnKindOneChar(SyntaxKind.OpenBraceToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnCloseParenToken() =>
        ReturnKindOneChar(SyntaxKind.CloseParenToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnOpenParenToken() =>
        ReturnKindOneChar(SyntaxKind.OpenParenToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnStarToken() =>
        ReturnKindOneChar(SyntaxKind.StarToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnSlashToken() =>
        ReturnKindOneChar(SyntaxKind.SlashToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnDashToken() =>
        ReturnKindOneChar(SyntaxKind.DashToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnDotToken() =>
        ReturnKindOneChar(SyntaxKind.DotToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnPlusToken() =>
        ReturnKindOneChar(SyntaxKind.PlusToken);

    private (SyntaxKind kind, int start, string text, object? value) ReturnEndOfInput() =>
        (SyntaxKind.EndOfInputToken, _position, string.Empty, null);

    private (SyntaxKind kind, int start, string text, object? value) ParseIdentOrKeyword()
    {
        var start = _position;
        Next(); // skip first letter
        while (Current != null && IsIdentCharacter(Current.Value, false))
        {
            Next();
        }

        var span = _file[start.._position];

        var kind = _getKeyword(span);

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
                Type.Int.ToPrintString()
            );

        return (SyntaxKind.NumberToken, start, span, value);
    }

    private SyntaxTrivia ParseLineComment()
    {
        var start = _position;

        Next(); // '/'
        Next(); // '/'
        while (Current != '\r' && Current != '\n' && Current != null)
        {
            Next();
        }

        return new SyntaxTrivia(
            _file,
            SyntaxKind.LineCommentTrivia,
            _file[start.._position],
            start
        );
    }

    private (SyntaxKind kind, int start, string text, object? value) ParseCharToken()
    {
        var start = _position;
        Next(); // start '
        char? c = null;
        switch (Current)
        {
            case '\\': // escape sequence
                var escapeSequence = ParseEscapeSequence();
                if (escapeSequence != null)
                    c = escapeSequence[0];

                break;
            case '\n':
            case '\r':
            case null:
                _diagnostics.ReportUnterminatedChar(
                    new TextLocation(_file, new TextSpan(start, 1))
                );

                break;

            default:
                c = Current.Value;
                Next();
                break;
        }

        if (Current == '\'')
        {
            Next();
        }
        else
        {
            _diagnostics.ReportUnterminatedChar(new TextLocation(_file, new TextSpan(start, 1)));
        }

        var span = _file[start.._position];

        if (c.HasValue)
            return (SyntaxKind.CharToken, start, span, c.Value);

        return (SyntaxKind.CharToken, start, span, null);
    }

    private (SyntaxKind kind, int start, string text, object value) ParseStringToken()
    {
        var start = _position;
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
                case null:
                    _diagnostics.ReportUnterminatedString(
                        new TextLocation(_file, new TextSpan(start, 1))
                    );
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
            case '\'':
                Next();
                return "\'";

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

            case '0':
                Next();
                return "\0";

            case 'u':
                Next(); //u
                return ParseUtfEscapeSequence(4, escapeStart);

            case 'U':
                Next(); //U
                return ParseUtfEscapeSequence(8, escapeStart);

            default:
                _diagnostics.ReportInvalidEscapeSequence(
                    new TextLocation(_file, new TextSpan(escapeStart, _position)),
                    Current!.Value
                );
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
                _diagnostics.ReportInvalidEscapeSequence(
                    new TextLocation(_file, new TextSpan(escapeStart, _position)),
                    Current!.Value
                );
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
            value = int.Parse(
                Current!.Value.ToString(),
                System.Globalization.NumberStyles.HexNumber
            );
            return true;
        }
        catch
        {
            value = 0;
            return false;
        }
    }

    private (SyntaxKind kind, int start, string text, object? value) ReturnKindTwoChar(
        SyntaxKind kind
    )
    {
        var start = _position;
        var text = _file[_position..(_position + 2)];
        _position += 2;
        return (kind, start, text, null);
    }

    private (SyntaxKind kind, int start, string text, object? value) ReturnKindOneChar(
        SyntaxKind kind
    )
    {
        var start = _position;
        var text = _file[_position..(_position + 1)];
        _position++;
        return (kind, start, text, null);
    }
}
