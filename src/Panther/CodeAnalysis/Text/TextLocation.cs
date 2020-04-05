using System;

namespace Panther.CodeAnalysis.Text
{
    public sealed class TextLocation : IComparable<TextLocation>, IComparable
    {
        public SourceText Text { get; }
        public TextSpan Span { get; }

        public string Filename => Text.FileName;
        public int StartLine => Text.GetLineIndex(Span.Start);
        public int StartCharacter => Span.Start - Text.Lines[StartLine].Start;
        public int EndLine => Text.GetLineIndex(Span.End);
        public int EndCharacter => Span.End - Text.Lines[StartLine].Start;

        public TextLocation(SourceText text, TextSpan span)
        {
            Text = text;
            Span = span;
        }

        public int CompareTo(TextLocation? other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            var filenameComparison = string.Compare(Filename, other.Filename, StringComparison.Ordinal);
            if (filenameComparison != 0) return filenameComparison;
            return Span.CompareTo(other.Span);
        }

        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            if (ReferenceEquals(this, obj)) return 0;
            return obj is TextLocation other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(TextLocation)}");
        }
    }
}