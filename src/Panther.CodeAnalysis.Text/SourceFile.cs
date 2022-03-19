using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Panther.CodeAnalysis.Text;

public abstract class SourceFile
{
    internal SourceFile(string fileName, string content)
    {
        FileName = fileName;
        Content = content;
    }

    public string FileName { get; }
    public string Content { get; }
    public virtual int LineCount => 0;
    public abstract TextLine GetLine(int index);

    public string this[Range range] => Content[range];
    public char this[int index] => Content[index];
    public int Length => Content.Length;

    public abstract int LineToOffset(int index);
    public abstract int GetLineIndex(int position);

    public static SourceFile From(string text, string fileName = "") => new ScriptSourceFile(text, fileName);
    public static SourceFile FromFile(string fileName) => new ScriptSourceFile(File.ReadAllText(fileName), fileName);

    public string ToString(int start, int length) => Content.Substring(start, length);

    public string ToString(TextSpan span) => ToString(span.Start, span.Length);

    public static readonly SourceFile Empty = new NoSourceFile();
}