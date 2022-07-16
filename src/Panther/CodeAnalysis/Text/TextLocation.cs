using System;

namespace Panther.CodeAnalysis.Text;

public class TextLocation : IEquatable<TextLocation>
{
    public SourceFile File { get; }
    public TextSpan Span { get; }

    public string Filename => File.FileName;
    public int StartLine => File.GetLineIndex(Span.Start);
    public int StartCharacter
    {
        get
        {
            var offset = File.LineToOffset(StartLine);
            return offset == -1 ? -1 : Span.Start - offset;
        }
    }

    public int EndLine => File.GetLineIndex(Span.End);
    public int EndCharacter
    {
        get
        {
            var offset = File.LineToOffset(StartLine);
            return offset == -1 ? -1 : Span.End - offset;
        }
    }

    public TextLocation(SourceFile file, TextSpan span)
    {
        File = file;
        Span = span;
    }

    public bool Equals(TextLocation? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;
        return File.Equals(other.File) && Span.Equals(other.Span);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != this.GetType())
            return false;
        return Equals((TextLocation)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(File, Span);
    }

    public static bool operator ==(TextLocation? left, TextLocation? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TextLocation? left, TextLocation? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return $"[{Span}]: {File.ToString(Span)}";
    }

    public static readonly TextLocation None = new(SourceFile.Empty, TextSpan.Empty);
}
