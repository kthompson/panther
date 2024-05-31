using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Mono.Cecil;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Authoring;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Panther.IO;
using SymbolFlags = Panther.CodeAnalysis.Binder.SymbolFlags;

namespace Panther.Repl;

internal class PantherRepl : Repl
{
    private Compilation? _previous;
    private bool _showTree;
    private bool _showProgram;

    private ScriptHost _scriptHost;

    public PantherRepl()
    {
        _scriptHost = BuildDefaultScriptHost();
        LoadSubmissions();
    }

    protected override object? RenderLine(IReadOnlyList<string> lines, int lineIndex, object? state)
    {
        SyntaxTree tree;
        if (state == null)
        {
            var text = string.Join(Environment.NewLine, lines);
            tree = SyntaxTree.Parse(text);
        }
        else
        {
            tree = (SyntaxTree)state;
        }

        if (tree.File is ScriptSourceFile sourceFile)
        {
            var lineSpan = sourceFile.GetLine(lineIndex).Span;
            var classifiedSpans = Classifier.Classify(tree, lineSpan);

            foreach (var classifiedSpan in classifiedSpans)
            {
                var text = tree.File.ToString(classifiedSpan.Span);

                Console.ForegroundColor = GetClassificationColor(classifiedSpan.Classification);
                Console.Write(text);
                Console.ResetColor();
            }
        }

        return tree;
    }

    private static ConsoleColor GetClassificationColor(Classification classification) =>
        classification switch
        {
            Classification.Keyword => ConsoleColor.Blue,
            Classification.Identifier => ConsoleColor.DarkYellow,
            Classification.Number => ConsoleColor.Cyan,
            Classification.String => ConsoleColor.Magenta,
            Classification.Comment => ConsoleColor.Green,
            Classification.Text => ConsoleColor.DarkGray,
            _ => ConsoleColor.DarkGray
        };

    [MetaCommand("exit", "Exit the repl environment")]
    private void MetaExit()
    {
        Running = false;
    }

    [MetaCommand("showProgram", "Toggle showing the bound tree")]
    private void MetaShowProgram()
    {
        _showProgram = !_showProgram;
        Console.WriteLine(_showProgram ? "Showing bound tree." : "Not showing bound tree.");
    }

    [MetaCommand("cls", "Clear the console")]
    private static void MetaClear()
    {
        Console.Clear();
    }

    [MetaCommand("reset", "Clear variables and all compilation data")]
    private void MetaReset()
    {
        _previous = null;
        _scriptHost.Dispose();
        _scriptHost = BuildDefaultScriptHost();
        ClearSubmissions();
    }

    private ScriptHost BuildDefaultScriptHost()
    {
        var result = LoadReferences();

        return new ScriptHost(result, null, "Repl");
    }

    private ImmutableArray<AssemblyDefinition>? _references;

    private ImmutableArray<AssemblyDefinition> LoadReferences()
    {
        if (_references != null)
            return _references.Value;

        var references = new string[]
        {
            typeof(object).Assembly.Location,
            typeof(Console).Assembly.Location,
            typeof(Unit).Assembly.Location
        };

        _references = references.Select(AssemblyDefinition.ReadAssembly).ToImmutableArray();

        return _references.Value;
    }

    [MetaCommand("showTree", "Toggle showing the parse tree")]
    private void MetaShowTree()
    {
        _showTree = !_showTree;
        Console.WriteLine(_showTree ? "Showing parse trees." : "Not showing parse trees.");
        return;
    }

    [MetaCommand("load", "Load a panther script file")]
    private void MetaLoad(string path)
    {
        if (!File.Exists(path))
        {
            WriteError("error: file does not exist");
            return;
        }

        var text = File.ReadAllText(path);
        EvaluateSubmission(text);
    }

    [MetaCommand("symbols", "Print the symbols in scope")]
    private void MetaDumpSymbols()
    {
        if (_previous == null)
            return;

        foreach (
            var symbol in _previous
                .GetSymbols()
                .Where(sym => !sym.Flags.HasFlag(SymbolFlags.Parameter))
        )
        {
            symbol.WriteTo(Console.Out);
            Console.WriteLine();
        }
    }

    [MetaCommand("dump", "Show bound tree of the given function")]
    // ReSharper disable once UnusedMember.Local
    private void MetaDumpFunction(string functionName)
    {
        if (_previous == null)
            return;

        var function = _previous
            .GetSymbols()
            .Where(m => m.Flags.HasFlag(SymbolFlags.Method))
            .FirstOrDefault(func => func.FullName == functionName);

        if (function == null)
        {
            WriteError($"Function {functionName} not found.");
            return;
        }

        // _previous.EmitTree(function, Console.Out);
    }

    protected override bool IsCompleteSubmission(string text)
    {
        if (string.IsNullOrEmpty(text))
            return true;

        // go ahead and show errors if the last two lines are blank
        var lastTwoLinesAreBlank = text.EndsWith(Environment.NewLine + Environment.NewLine);
        if (lastTwoLinesAreBlank)
            return true;

        var syntaxTree = SyntaxTree.Parse(text.TrimEnd('\r', '\n'));

        if (syntaxTree.Diagnostics.Any())
            return false;

        return true;
    }

    protected override void EvaluateSubmission(string text)
    {
        try
        {
            var syntaxTree = SyntaxTree.Parse(text.TrimEnd('\r', '\n'));
            var compilation = _scriptHost.Compile(syntaxTree);

            if (_showTree)
                syntaxTree.Root.WriteTo(Console.Out);

            if (_showProgram)
                compilation.EmitTree(Console.Out);

            var result = _scriptHost.Execute(compilation);

            if (result.Diagnostics.Any())
            {
                Console.Error.WriteDiagnostics(result.Diagnostics);
                Console.WriteLine();
                return;
            }

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(result.Value);
            Console.ResetColor();
            _previous = compilation;

            SaveSubmission(text);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static readonly string SubmissionsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Panther",
        "Submissions"
    );

    private bool _saveSubmissions = false;

    private void LoadSubmissions()
    {
        _saveSubmissions = false;

        try
        {
            if (!Directory.Exists(SubmissionsFolder))
                return;

            var files = Directory.GetFiles(SubmissionsFolder).OrderBy(file => file).ToArray();
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                EvaluateSubmission(text);
            }

            if (files.Length <= 0)
                return;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine($"Loaded {files.Length} submissions");
            Console.ResetColor();
        }
        finally
        {
            _saveSubmissions = true;
        }
    }

    private void ClearSubmissions()
    {
        if (Directory.Exists(SubmissionsFolder))
        {
            Directory.Delete(SubmissionsFolder, true);
        }
    }

    private void SaveSubmission(string text)
    {
        if (!_saveSubmissions)
            return;

        Directory.CreateDirectory(SubmissionsFolder);
        var count = Directory.GetFiles(SubmissionsFolder).Length;
        var name = $"submission-{count:0000}.pn";
        var submissionFile = Path.Combine(SubmissionsFolder, name);
        File.WriteAllText(submissionFile, text);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        this._scriptHost.Dispose();

        if (_references == null)
            return;

        foreach (var reference in _references.Value)
        {
            reference.Dispose();
        }
    }
}
