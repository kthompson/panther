using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics;
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

            var rootCommand = new RootCommand
            {
                new Option<FileInfo>(aliases: new []{"/output", "/o"}, description: "The output path of the assembly to create")
                {
                    Required = true,
                },
                new Option<FileInfo[]>(aliases: new[] {"/reference", "/r"}, description: "An assembly reference").ExistingOnly(),
                new Option<string>(aliases: new[] {"/module", "/m"}, getDefaultValue: () => "main", description: "The module name"),
                new Argument<FileInfo[]>("sources").ExistingOnly()
            };

            rootCommand.Handler = CommandHandler.Create(
                (FileInfo output, FileInfo[] reference, string module, FileInfo[] sources, ParseResult parseResult) =>
                {
                    if (sources.Length == 0)
                    {
                        Console.Error.WriteLine("usage: pantherc <source-paths>");
                        return 1;
                    }

                    var syntaxTrees = sources.Select(source => SyntaxTree.LoadFile(source.FullName)).ToArray();
                    var compilation = Compilation.Create(syntaxTrees);

                    var diagnostics = compilation.Emit(module, reference.Select(x => x.FullName).ToArray(), output.FullName);

                    if (diagnostics.Any())
                    {
                        Console.Error.WriteDiagnostics(diagnostics);
                        return 1;
                    }

                    //
                    // var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());
                    // if (result.Diagnostics.Any())
                    // {
                    //     Console.Error.WriteDiagnostics(result.Diagnostics);
                    //     return 1;
                    // }
                    //
                    // if (result.Value != null)
                    //     Console.WriteLine(result.Value);

                    return 0;
                });

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}