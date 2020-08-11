namespace Panther.CodeAnalysis.Text
{
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