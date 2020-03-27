﻿using System;
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

                case '{':
                    return ReturnKindOneChar(SyntaxKind.OpenBraceToken);

                case '}':
                    return ReturnKindOneChar(SyntaxKind.CloseBraceToken);
                
                case '^':
                    return ReturnKindOneChar(SyntaxKind.CaretToken);

                case '>':
                    return Lookahead == '='
                        ? ReturnKindTwoChar(SyntaxKind.GreaterThanEqualsToken)
                        : ReturnKindOneChar(SyntaxKind.GreaterThanToken);

                case '<':
                    return Lookahead == '='
                        ? ReturnKindTwoChar(SyntaxKind.LessThanEqualsToken)
                        : ReturnKindOneChar(SyntaxKind.LessThanToken);

                case '!':
                    return Lookahead == '='
                        ? ReturnKindTwoChar(SyntaxKind.BangEqualsToken)
                        : ReturnKindOneChar(SyntaxKind.BangToken);

                case '&' :
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
                                typeof(int));

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