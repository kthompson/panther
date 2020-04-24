using System;

namespace Panther.CodeAnalysis.Text
{
    public struct TextLocation
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

        public bool Equals(TextLocation other)
        {
            return Text.Equals(other.Text) && Span.Equals(other.Span);
        }

        public override bool Equals(object? obj)
        {
            return obj is TextLocation other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text, Span);
        }

        public static bool operator ==(TextLocation left, TextLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(TextLocation left, TextLocation right)
        {
            return !left.Equals(right);
        }
    }
}