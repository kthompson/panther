using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Syntax;
using Panther.IO;
using Panther.Repl;

if (args.Length > 0 && args[0] == "console")
{
    using var repl = new PantherRepl();
    repl.Run();
    return 0;
}

var sources = new List<string>();
var references = new List<string>();
string? output = null;
var module = "main";

for (var i = 0; i < args.Length; i++)
{
    var arg = args[i];
    if (CheckArg(arg, "o", "output"))
    {
        if (output != null)
        {
            PrintError("output defined twice");
            return -1;
        }

        if (i + 1 >= args.Length)
        {
            PrintError("expected output parameter");
            return -1;
        }

        output = args[i + 1];
        i++;
        continue;
    }

    if (CheckArg(arg, "m", "module"))
    {
        if (i + 1 >= args.Length)
        {
            PrintError("expected module parameter");
            return -1;
        }

        module = args[i + 1];
        i++;
        continue;
    }

    if (CheckArg(arg, "r", "reference"))
    {
        if (i + 1 >= args.Length)
        {
            PrintError("expected reference parameter");
            return -1;
        }

        references.Add(args[i + 1]);
        i++;
        continue;
    }

    if (CheckArg(arg, "b", "break"))
    {
        i++;
        Debugger.Launch();
        continue;
    }

    if (arg.StartsWith("/") || arg.StartsWith("-"))
    {
        PrintError($"unexpected option {arg}");
        return -1;
    }

    sources.Add(arg);
}

if (!ValidateSources())
    return -1;
if (!ValidateReferences())
    return -1;
if (output == null)
{
    PrintError("output not defined");
    return -1;
}

var syntaxTrees = sources
    .Select(file =>
    {
        Console.WriteLine($"parsing file {file}...");
        return SyntaxTree.LoadFile(file);
    })
    .ToArray();

var (referenceDiags, compilation) = Compilation.Create(references.ToArray(), syntaxTrees);

if (referenceDiags.Any() || compilation == null)
{
    Console.Error.WriteDiagnostics(referenceDiags);
    return 1;
}

var result = compilation.Emit(module, output);
if (result.Diagnostics.Any())
{
    Console.Error.WriteDiagnostics(result.Diagnostics);
    return 1;
}

return 0;

bool ValidateReferences()
{
    for (var i = 0; i < references.Count; i++)
    {
        var reference = references[i];
        var info = new FileInfo(reference);
        if (!info.Exists)
        {
            PrintError($"invalid reference: {reference}");
            return false;
        }

        references[i] = info.FullName;
    }

    return true;
}

bool ValidateSources()
{
    if (sources.Count == 0)
    {
        PrintError($"no sources provided");
        return false;
    }

    for (var i = 0; i < sources.Count; i++)
    {
        var source = sources[i];
        var info = new FileInfo(source);
        if (!info.Exists)
        {
            PrintError($"invalid source: {source}");
            return false;
        }

        sources[i] = info.FullName;
    }

    return true;
}

bool CheckArg(string arg, string shortName, string longName) =>
    arg == $"/{shortName}"
    || arg == $"/{longName}"
    || arg == $"-{shortName}"
    || arg == $"--{longName}";

void PrintError(string error)
{
    Console.Error.WriteLine($"Error: {error}");
    Console.Error.WriteLine();
    PrintUsage();
}

void PrintUsage() =>
    Console.Error.WriteLine(
        $@"
usage: pnc <source-paths> /o <output> /r <reference> [/m <module>]
version: {ThisAssembly.AssemblyInformationalVersion}

    -o, --output       Required. The output path of the assembly to create

    -r, --reference    Required. An assembly reference

    -m, --module       Required. (Default: main) The module name

    --help             Display this help screen.

    --version          Display version information.
"
    );
