using System;
using System.Linq;
using Panther.CodeAnalysis;

namespace Panther
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var showTree = false;
            var color = Console.ForegroundColor;

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

                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    PrettyPrint(syntaxTree.Root);
                }

                var diags = syntaxTree.Diagnostics;
                if (diags.Any())
                {
                    Console.ForegroundColor = ConsoleColor.DarkRed;

                    foreach (var diag in diags)
                    {
                        Console.WriteLine(diag);
                    }
                }
                else
                {
                    Console.ForegroundColor = color;

                    var eval = new Evaluator(syntaxTree.Root);
                    var result = eval.Evaluate();
                    Console.WriteLine(result);
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