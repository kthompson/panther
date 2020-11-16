namespace Panther.CodeAnalysis.Text
{
    public sealed class TextLine
    {
        public SourceFile File { get; }
        public int Start { get; }
        public int Length { get; }
        public int LengthIncludingLineBreaks { get; }
        public int End => Start + Length;
        public TextSpan Span => new TextSpan(Start, Length);
        public TextSpan SpanIncludingLineBreak => new TextSpan(Start, LengthIncludingLineBreaks);

        public TextLine(SourceFile file, int start, int length, int lengthIncludingLineBreaks)
        {
            File = file;
            Start = start;
            Length = length;
            LengthIncludingLineBreaks = lengthIncludingLineBreaks;
        }

        public override string ToString() => File.ToString(Span);
    }
}