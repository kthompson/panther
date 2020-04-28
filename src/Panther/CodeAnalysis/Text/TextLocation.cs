using System;

namespace Panther.CodeAnalysis.Text
{
    public class TextLocation : IEquatable<TextLocation>
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

        public bool Equals(TextLocation? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Text.Equals(other.Text) && Span.Equals(other.Span);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TextLocation) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Text, Span);
        }

        public static bool operator ==(TextLocation? left, TextLocation? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(TextLocation? left, TextLocation? right)
        {
            return !Equals(left, right);
        }

    }
}