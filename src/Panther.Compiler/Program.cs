using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.IO;

namespace Panther.Compiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("usage: pantherc <source-paths>");
                return;
            }

            if (args.Length > 1)
            {
                Console.Error.WriteLine("error: only one source file currently supported");
                return;
            }

            var path = args.Single();


            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"error: file `{path}` not found");
                return;
            }
            var syntaxTree = SyntaxTree.LoadFile(path);

            var compilation = new Compilation(syntaxTree);
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());
            if (result.Diagnostics.Any())
            {
                Console.Error.WriteDiagnostics(result.Diagnostics, syntaxTree);
            }
            else if (result.Value != null)
            {
                Console.WriteLine(result.Value);
            }
        }
    }
}