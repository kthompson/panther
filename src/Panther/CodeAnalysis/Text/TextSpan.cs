using System;

namespace Panther.CodeAnalysis.Text
{
    public struct TextSpan : IComparable<TextSpan>, IComparable
    {
        public int Start { get; }
        public int Length { get; }
        public int End => Start + Length;

        public TextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public static TextSpan FromBounds(in int start, in int end) => new TextSpan(start, end - start);

        public override string ToString()
        {
            return $"{Start}..{End}";
        }

        public int CompareTo(TextSpan other)
        {
            var startComparison = Start.CompareTo(other.Start);
            return startComparison != 0 ? startComparison : Length.CompareTo(other.Length);
        }

        public int CompareTo(object? obj)
        {
            if (ReferenceEquals(null, obj)) return 1;
            return obj is TextSpan other ? CompareTo(other) : throw new ArgumentException($"Object must be of type {nameof(TextSpan)}");
        }
    }
}