using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Text;
using Panther.CodeAnalysis.Text;

namespace Panther.Tests.CodeAnalysis
{
    internal sealed class AnnotatedText
    {
        public string Text { get; }
        public ImmutableArray<TextSpan> Spans { get; }

        public AnnotatedText(string text, ImmutableArray<TextSpan> spans)
        {
            Text = text;
            Spans = spans;
        }

        public static AnnotatedText Parse(string text)
        {
            text = StripMargin(text);

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

        private static string StripMargin(string text)
        {
            var lines = UnindentLines(text);

            return string.Join(Environment.NewLine, lines);
        }

        public static string[] UnindentLines(string text)
        {
            var lines = new List<string>();

            using var reader = new StringReader(text);

            string readLine;
            while ((readLine = reader.ReadLine()) != null)
            {
                lines.Add(readLine);
            }

            var minIndentation = int.MaxValue;
            for (var index = 0; index < lines.Count; index++)
            {
                var line = lines[index];
                if (line.Trim().Length == 0)
                {
                    lines[index] = string.Empty;
                    continue;
                }

                var indentation = line.Length - line.TrimStart().Length;
                minIndentation = Math.Min(minIndentation, indentation);
            }

            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i] == string.Empty)
                    continue;

                lines[i] = lines[i].Substring(minIndentation);
            }

            while (lines.Count > 0 && lines[0].Length == 0)
                lines.RemoveAt(0);

            while (lines.Count > 0 && lines[^1].Length == 0)
                lines.RemoveAt(lines.Count - 1);

            return lines.ToArray();
        }
    }
}