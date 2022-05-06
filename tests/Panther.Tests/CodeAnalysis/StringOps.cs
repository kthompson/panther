using System;
using System.Collections.Generic;
using System.IO;

namespace Panther.Tests.CodeAnalysis;

static class StringOps
{
    public static string StripMargin(this string text)
    {
        var lines = UnindentLines(text);

        return string.Join(Environment.NewLine, lines);
    }

    public static string[] UnindentLines(this string text)
    {
        var lines = new List<string>();

        using var reader = new StringReader(text);

        string? readLine;
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

        for (var i = 0; i < lines.Count; i++)
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