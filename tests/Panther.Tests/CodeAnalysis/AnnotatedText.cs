using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Panther.CodeAnalysis.Text;

namespace Panther.Tests.CodeAnalysis;

internal sealed class AnnotatedText
{
    public string Text { get; }
    public SourceFile File { get; }

    public ImmutableArray<TextLocation> Locations { get; }
    public ImmutableArray<TextSpan> Spans { get; }

    private AnnotatedText(string text, ImmutableArray<TextSpan> spans)
    {
        Text = text;
        File = new ScriptSourceFile(text, "annotated-text");
        Spans = spans;
        Locations = Spans.Select(span => new TextLocation(File, span)).ToImmutableArray();
    }

    public static AnnotatedText Parse(string text)
    {
        text = text.StripMargin();

        var builder = new StringBuilder();
        var spans = ImmutableArray.CreateBuilder<TextSpan>();
        var startStack = new Stack<int>();
        var position = 0;

        foreach (var c in text)
        {
            if (c == '[')
            {
                startStack.Push(position);
            }
            else if (c == ']')
            {
                if (startStack.Count == 0)
                    throw new ArgumentException("Too many ']' in text", nameof(text));

                var start = startStack.Pop();
                var end = position;
                var span = TextSpan.FromBounds(start, end);
                spans.Add(span);
            }
            else
            {
                position++;
                builder.Append(c);
            }
        }

        if (startStack.Count != 0)
            throw new ArgumentException("Missing ']' in text", nameof(text));

        return new AnnotatedText(builder.ToString(), spans.ToImmutable());
    }
}
