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
                var textLocation = diagnostic.Location;
                if (textLocation == null)
                {
                    writer.WriteLine();

                    writer.SetForeground(ConsoleColor.DarkRed);
                    writer.Write($"Error: ");
                    writer.WriteLine(diagnostic);
                    writer.ResetColor();
                }
                else
                {
                    var span = textLocation.Span;
                    var text = textLocation.File;
                    var fileName = textLocation.Filename ?? "";
                    var startLine = textLocation.StartLine + 1;
                    var startCharacter = textLocation.StartCharacter + 1;

                    writer.WriteLine();

                    writer.SetForeground(ConsoleColor.DarkRed);
                    writer.Write($"{fileName}({startLine},{startCharacter}): Error PN0000 : ");
                    writer.WriteLine(diagnostic);
                    writer.ResetColor();

                    if (!(text is ScriptSourceFile sourceFile))
                        continue;

                    for (int currentLine = textLocation.StartLine;
                        currentLine <= textLocation.EndLine;
                        currentLine++)
                    {

                        var line = sourceFile.GetLine(currentLine);
                        var startInCurrentLine = text.GetLineIndex(span.Start) == currentLine;
                        var endInCurrentLine = text.GetLineIndex(span.End) == currentLine;

                        var prefixEnd = startInCurrentLine ? span.Start : line.Start;
                        var suffixStart = endInCurrentLine ? span.End : line.End;

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
}