﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;

namespace Panther
{
    internal class Repl
    {
        private static void Main()
        {
            var showTree = false;
            var showTypes = true;
            var variables = new Dictionary<VariableSymbol, object>();
            var code = new StringBuilder();
            Compilation previous = null;

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write(code.Length == 0 ? "> " : "| ");
                Console.ResetColor();

                var input = Console.ReadLine();
                var isBlank = string.IsNullOrWhiteSpace(input);

                if (code.Length == 0)
                {
                    if (isBlank)
                        return;

                    if (input == "#showTree")
                    {
                        showTree = !showTree;
                        Console.WriteLine(showTree ? "Showing parse tree" : "Not showing parse tree");
                        continue;
                    }

                    if (input == "#showTypes")
                    {
                        showTypes = !showTypes;
                        Console.WriteLine(showTypes ? "Showing type tree" : "Not showing type tree");
                        continue;
                    }

                    if (input == "#cls")
                    {
                        Console.Clear();
                        continue;
                    }

                    if (input == "#reload")
                    {
                        previous = null;
                        continue;
                    }
                }

                code.AppendLine(input);

                var syntaxTree = SyntaxTree.Parse(code.ToString());

                if (!isBlank && syntaxTree.Diagnostics.Any())
                {
                    continue;
                }

                var compilation = previous == null ? new Compilation(syntaxTree) : previous.ContinueWith(syntaxTree);
                if (showTree)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    syntaxTree.Root.WriteTo(Console.Out);
                }

                if (showTypes)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    compilation.EmitTree(Console.Out);
                }

                var result = compilation.Evaluate(variables);
                var diags = result.Diagnostics;

               
                if (diags.Any())
                {
                    var text = syntaxTree.Text;
                    foreach (var diag in diags)
                    {
                        var lineIndex = text.GetLineIndex(diag.Span.Start);
                        var lineNumber = lineIndex + 1;
                        var line = text.Lines[lineIndex];
                        var character = diag.Span.Start - line.Start + 1;

                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write($"({lineNumber}, {character}) ");
                        Console.WriteLine(diag);
                        Console.ResetColor();

                        var prefixSpan = TextSpan.FromBounds(line.Start, diag.Span.Start);
                        var suffixSpan = TextSpan.FromBounds(diag.Span.End, line.End);

                        var prefix = text.ToString(prefixSpan);
                        var error = text.ToString(diag.Span);
                        var suffix = text.ToString(suffixSpan);

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
                    previous = compilation;

                    Console.ForegroundColor = ConsoleColor.Magenta;

                    Console.WriteLine(result.Value);
                }

                code.Clear();
            }
        }
    }
}