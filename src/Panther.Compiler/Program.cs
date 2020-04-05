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
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("usage: pantherc <source-paths>");
                return 1;
            }

            var paths = GetFilePaths(args).ToArray();
            var syntaxTrees = new List<SyntaxTree>(args.Length);

            var errors = false;
            foreach (var path in paths)
            {
                if (!File.Exists(path))
                {
                    Console.Error.WriteLine($"error: file `{path}` not found");
                    errors = true;
                    continue;
                }

                var syntaxTree = SyntaxTree.LoadFile(path);
                syntaxTrees.Add(syntaxTree);
            }

            if (errors)
                return 1;

            var compilation = new Compilation(syntaxTrees.ToArray());
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());
            if (result.Diagnostics.Any())
            {
                Console.Error.WriteDiagnostics(result.Diagnostics);
                return 1;
            }

            if (result.Value != null)
                Console.WriteLine(result.Value);

            return 0;
        }

        private static IEnumerable<string> GetFilePaths(IEnumerable<string> paths)
        {
            var results = new SortedSet<string>();

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    results.UnionWith(Directory.EnumerateFiles(path, "*.pn", SearchOption.AllDirectories));
                }
                else
                {
                    results.Add(path);
                }
            }

            return results;
        }
    }
}