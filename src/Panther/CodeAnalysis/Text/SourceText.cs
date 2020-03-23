using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Panther.CodeAnalysis.Text
{
    public sealed class SourceText
    {
        private readonly string _text;
        public IReadOnlyList<TextLine> Lines { get; }

        private SourceText(string text)
        {
            _text = text;
            Lines = ParseLines(this, text);
        }

        public string this[Range range] => _text[range];
        public char this[int index] => _text[index];
        public int Length => _text.Length;

        public int GetLineIndex(int position)
        {
            var lower = 0;
            var upper = Lines.Count - 1;

            while (lower <= upper)
            {
                var index = lower + (upper - lower) / 2;
                var start = Lines[index].Start;

                if (position == start)
                    return index;

                if (start > position)
                {
                    upper = index - 1;
                }
                else
                {
                    lower = index + 1;
                }
            }

            return lower - 1;
        }

        private static IReadOnlyList<TextLine> ParseLines(SourceText sourceText, string text)
        {
            var list = new List<TextLine>();
            var position = 0;
            var lineStart = 0;

            while (position < text.Length)
            {
                var lineBreakWidth = GetLineBreakWidth(text, position);
                if (lineBreakWidth == 0)
                {
                    position++;
                }
                else
                {
                    AddLine(list, sourceText, position, lineStart, lineBreakWidth);

                    position += lineBreakWidth;
                    lineBreakWidth = position;
                }
            }

            if (position > lineStart)
            {
                AddLine(list, sourceText, position, lineStart, 0);
            }

            return list;
        }

        private static void AddLine(ICollection<TextLine> list, SourceText sourceText, in int position, in int lineStart, in int lineBreakWidth)
        {
            var lineLength = position - lineStart;
            var lineLengthIncludingLineBreak = lineStart + lineBreakWidth;
            var line = new TextLine(sourceText, lineStart, lineLength, lineLengthIncludingLineBreak);
            list.Add(line);
        }

        private static int GetLineBreakWidth(string text, in int position)
        {
            var c = text[position];
            var l = position + 1 >= text.Length ? '\0' : text[position + 1];

            if (c == '\r' && l == '\n')
                return 2;

            if (c == '\r' || l == '\n')
                return 1;

            return 0;
        }

        public static SourceText From(string text) => new SourceText(text);

        public override string ToString() => _text;

        public string ToString(int start, int length) => _text.Substring(start, length);

        public string ToString(TextSpan span) => ToString(span.Start, span.Length);
    }

    public sealed class TextLine
    {
        public SourceText Text { get; }
        public int Start { get; }
        public int Length { get; }
        public int LengthIncludingLineBreaks { get; }
        public int End => Start + Length;
        public TextSpan Span => new TextSpan(Start, Length);
        public TextSpan SpanIncludingLineBreak => new TextSpan(Start, LengthIncludingLineBreaks);

        public TextLine(SourceText text, int start, int length, int lengthIncludingLineBreaks)
        {
            Text = text;
            Start = start;
            Length = length;
            LengthIncludingLineBreaks = lengthIncludingLineBreaks;
        }

        public override string ToString() => Text.ToString(Span);
    }
}