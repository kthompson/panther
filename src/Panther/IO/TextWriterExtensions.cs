using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther.IO
{
    public static class TextWriterExtensions
    {
        public static bool IsConsole(this TextWriter writer)
        {
            if (writer == Console.Out)
                return !Console.IsOutputRedirected;

            if (writer == Console.Error)
                return !Console.IsErrorRedirected && !Console.IsOutputRedirected;

            if (!(writer is IndentedTextWriter indentedTextWriter))
                return false;

            return indentedTextWriter.InnerWriter.IsConsole();
        }

        public static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (writer.IsConsole())
                Console.ForegroundColor = color;
        }

        public static void ResetColor(this TextWriter writer)
        {
            if (writer.IsConsole())
                Console.ResetColor();
        }

        public static void WriteKeyword(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Blue);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteString(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Magenta);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteNumber(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.Cyan);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteIdentifier(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.DarkYellow);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WritePunctuation(this TextWriter writer, string text)
        {
            writer.SetForeground(ConsoleColor.DarkGray);
            writer.Write(text);
            writer.ResetColor();
        }

        public static void WriteDiagnostics(this TextWriter writer, IEnumerable<Diagnostic> diagnostics)
        {
            foreach (var diagnostic in diagnostics.OrderBy(diagnostic => diagnostic.Span))
            {
                var text = diagnostic.Location.Text;
                var fileName = diagnostic.Location.Filename;
                var startLine = diagnostic.Location.StartLine + 1;
                var startCharacter = diagnostic.Location.StartCharacter + 1;
                var endLine = diagnostic.Location.EndLine + 1;
                var endCharacter = diagnostic.Location.EndCharacter + 1;


                writer.WriteLine();

                writer.SetForeground(ConsoleColor.DarkRed);
                writer.Write($"{fileName}({startLine},{startCharacter},{endLine},{endCharacter}): ");
                writer.WriteLine(diagnostic);
                writer.ResetColor();

                for (int currentLine = diagnostic.Location.StartLine; currentLine <= diagnostic.Location.EndLine; currentLine++)
                {
                    var line = text.Lines[currentLine];
                    var startInCurrentLine = text.GetLineIndex(diagnostic.Span.Start) == currentLine;
                    var endInCurrentLine = text.GetLineIndex(diagnostic.Span.End) == currentLine;

                    var prefixEnd = startInCurrentLine ? diagnostic.Span.Start : line.Start;
                    var suffixStart = endInCurrentLine ? diagnostic.Span.End : line.End;

                    var prefixSpan = TextSpan.FromBounds(line.Start, prefixEnd);
                    var errorSpan = TextSpan.FromBounds(prefixEnd, suffixStart);
                    var suffixSpan = TextSpan.FromBounds(suffixStart, line.End);

                    var prefix = text.ToString(prefixSpan);
                    var error = text.ToString(errorSpan);
                    var suffix = text.ToString(suffixSpan);

                    writer.Write("  ");
                    writer.Write(prefix);

                    writer.SetForeground(ConsoleColor.DarkRed);
                    writer.Write(error);
                    writer.ResetColor();

                    writer.Write(suffix);

                    writer.WriteLine();
                }
            }
        }
    }
}