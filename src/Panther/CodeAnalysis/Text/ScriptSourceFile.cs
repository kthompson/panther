using System.Collections.Generic;

namespace Panther.CodeAnalysis.Text;

public sealed class ScriptSourceFile : SourceFile
{
    public override int LineCount => Lines.Count;
    private IReadOnlyList<TextLine> Lines { get; }

    internal ScriptSourceFile(string text, string fileName)
        : base(fileName, text)
    {
        Lines = ParseLines(this, text);
    }

    public override int LineToOffset(int index) => Lines[index].Start;

    public override int GetLineIndex(int position)
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

    public override TextLine GetLine(int index) => this.Lines[index];

    private static IReadOnlyList<TextLine> ParseLines(SourceFile sourceFile, string text)
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
                AddLine(list, sourceFile, position, lineStart, lineBreakWidth);

                position += lineBreakWidth;
                lineStart = position;
            }
        }

        if (position >= lineStart)
        {
            AddLine(list, sourceFile, position, lineStart, 0);
        }

        return list;
    }

    private static void AddLine(ICollection<TextLine> list, SourceFile sourceFile, in int position, in int lineStart, in int lineBreakWidth)
    {
        var lineLength = position - lineStart;
        var lineLengthIncludingLineBreak = lineStart + lineBreakWidth;
        var line = new TextLine(sourceFile, lineStart, lineLength, lineLengthIncludingLineBreak);
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

    public override string ToString() => this.Content;
}