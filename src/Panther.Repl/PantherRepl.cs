using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Panther.IO;

namespace Panther
{
    internal class PantherRepl : Repl
    {
        private Compilation? _previous;
        private bool _showTree;
        private bool _showProgram;
        private readonly Dictionary<VariableSymbol, object> _variables = new Dictionary<VariableSymbol, object>();

        public PantherRepl()
        {
            LoadSubmissions();
        }

        protected override void RenderLine(string line)
        {
            var tokens = SyntaxTree.ParseTokens(line);
            foreach (var token in tokens)
            {
                Console.ForegroundColor = GetTokenColor(token);

                Console.Write(token.Text);

                Console.ResetColor();
            }
        }

        private static ConsoleColor GetTokenColor(SyntaxToken token) =>
            token.Kind switch
            {
                SyntaxKind.IdentifierToken => ConsoleColor.DarkYellow,
                SyntaxKind.StringToken => ConsoleColor.Magenta,
                SyntaxKind.NumberToken => ConsoleColor.Cyan,
                SyntaxKind kind when kind.ToString().EndsWith("Keyword") => ConsoleColor.Blue,
                _ => ConsoleColor.DarkGray
            };

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
            _variables.Clear();
            ClearSubmissions();
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

            foreach (var symbol in _previous.GetSymbols().OrderBy(s => s.Kind).ThenBy(s => s.Name))
            {
                symbol.WriteTo(Console.Out);
                Console.WriteLine();
            }
        }

        [MetaCommand("dump", "Show bound tree of the given function")]
        private void MetaDumpFunction(string functionName)
        {
            var function = _previous.GetSymbols().OfType<FunctionSymbol>().FirstOrDefault(func => func.Name == functionName);

            if (function == null)
            {
                WriteError($"Function {functionName} not found.");
                return;
            }

            _previous.EmitTree(function, Console.Out);
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
            var syntaxTree = SyntaxTree.Parse(text.TrimEnd('\r', '\n'));
            var compilation = Compilation.CreateScript(_previous, syntaxTree);

            if (_showTree)
                syntaxTree.Root.WriteTo(Console.Out);

            if (_showProgram)
                compilation.EmitTree(Console.Out);

            var result = compilation.Evaluate(_variables);

            if (!result.Diagnostics.Any())
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine(result.Value);
                Console.ResetColor();
                _previous = compilation;

                SaveSubmission(text);
            }
            else
            {
                Console.Error.WriteDiagnostics(result.Diagnostics);
                Console.WriteLine();
            }
        }

        private static readonly string SubmissionsFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Panther",
                "Submissions");

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

                if (files.Length <= 0) return;

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
            Directory.Delete(SubmissionsFolder, true);
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
    }
}