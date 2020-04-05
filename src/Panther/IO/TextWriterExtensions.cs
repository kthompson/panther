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
        public static bool IsConsoleOut(this TextWriter writer) =>
            writer == Console.Out ||
            writer is IndentedTextWriter indentedTextWriter && indentedTextWriter.InnerWriter == Console.Out;

        public static void SetForeground(this TextWriter writer, ConsoleColor color)
        {
            if (writer.IsConsoleOut())
                Console.ForegroundColor = color;
        }

        public static void ResetColor(this TextWriter writer)
        {
            if (writer.IsConsoleOut())
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

        public static void WriteIdentifer(this TextWriter writer, string text)
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

        public static void WriteDiagnostics(this TextWriter writer, IEnumerable<Diagnostic> diagnostics, SyntaxTree syntaxTree)
        {
            foreach (var diagnostic in diagnostics.OrderBy(diagnostic => diagnostic.Span))
            {
                var fileName = diagnostic.Location.Filename;
                var startLine = diagnostic.Location.StartLine + 1;
                var startCharacter = diagnostic.Location.StartCharacter + 1;
                var endLine = diagnostic.Location.EndLine + 1;
                var endCharacter = diagnostic.Location.EndCharacter + 1;

                var lineIndex = syntaxTree.Text.GetLineIndex(diagnostic.Span.Start);
                var line = syntaxTree.Text.Lines[lineIndex];

                Console.WriteLine();

                Console.ForegroundColor = ConsoleColor.DarkRed;
                writer.Write($"{fileName}({startLine},{startCharacter},{endLine},{endCharacter}): ");
                writer.WriteLine(diagnostic);
                writer.ResetColor();

                var prefixSpan = TextSpan.FromBounds(line.Start, diagnostic.Span.Start);
                var suffixSpan = TextSpan.FromBounds(diagnostic.Span.End, line.End);

                var prefix = syntaxTree.Text.ToString(prefixSpan);
                var error = syntaxTree.Text.ToString(diagnostic.Span);
                var suffix = syntaxTree.Text.ToString(suffixSpan);

                writer.Write("    ");
                writer.Write(prefix);

                Console.ForegroundColor = ConsoleColor.DarkRed;
                writer.Write(error);
                writer.ResetColor();

                writer.Write(suffix);

                writer.WriteLine();
            }
        }
    }
}