using System;
using System.Collections.Generic;
using System.Linq;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;

namespace Panther
{
    internal class Program
    {
        private static void Main()
        {
            var showTree = false;
            var color = Console.ForegroundColor;
            var variables = new Dictionary<VariableSymbol, object>();

            while (true)
            {
                Console.ForegroundColor = color;
                Console.Write("> ");
                var line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    return;

                if (line == "#showTree")
                {
                    showTree = !showTree;
                    Console.WriteLine(showTree ? "Showing parse tree" : "Not showing parse tree");
                    continue;
                }

                if (line == "#cls")
                {
                    Console.Clear();
                    continue;
                }

                var syntaxTree = SyntaxTree.Parse(line);
                var compilation = new Compilation(syntaxTree);
                var result = compilation.Evaluate(variables);
                var diags = result.Diagnostics;

                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrettyPrint(syntaxTree.Root);
                }

                if (diags.Any())
                {
                    foreach (var diag in diags)
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.WriteLine(diag);
                        Console.ResetColor();

                        var prefix = line.Substring(0, diag.Span.Start);
                        var error = line.Substring(diag.Span.Start, diag.Span.Length);
                        var suffix = line.Substring(diag.Span.End);

                        Console.Write("    ");
                        Console.Write(prefix);
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write(error);
                        Console.ResetColor();
                        Console.Write(suffix);
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.ForegroundColor = color;

                    Console.WriteLine(result.Value);
                }
            }
        }

        private static void PrettyPrint(SyntaxNode node, string indent = "", bool isLast = true)
        {
            // ├──
            // │
            // └──

            var marker = isLast ? "└──" : "├──";

            Console.Write(indent);
            Console.Write(marker);
            Console.Write(node.Kind);

            if (node is SyntaxToken t && t.Value != null)
            {
                Console.Write(" ");
                Console.Write(t.Value);
            }

            Console.WriteLine();

            indent += isLast ? "    " : "│   ";

            using var enumerator = node.GetChildren().GetEnumerator();
            if (enumerator.MoveNext())
            {
                var previous = enumerator.Current;

                while (enumerator.MoveNext())
                {
                    PrettyPrint(previous, indent, false);
                    previous = enumerator.Current;
                }

                PrettyPrint(previous, indent);
            }
        }
    }
}