using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Panther.IO;

namespace Panther
{
    internal abstract class Repl
    {
        private readonly List<MetaCommand> _metaCommands = new List<MetaCommand>();
        private readonly List<string> _submissionHistory = new List<string>();
        private int _submissionHistoryIndex;

        private bool _done;

        private protected bool Running = true;

        protected Repl()
        {
            InitializeMetaCommands();
        }

        private void InitializeMetaCommands()
        {
            foreach (var method in GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                var attribute = method.GetCustomAttribute<MetaCommandAttribute>();
                if (attribute == null)
                    continue;

                _metaCommands.Add(new MetaCommand(attribute.Name, attribute.Description, method));
            }
        }

        public void Run()
        {
            while (Running)
            {
                var text = EditSubmission();
                if (string.IsNullOrEmpty(text))
                    continue;

                if (!text.Contains(Environment.NewLine) && text.StartsWith("#"))
                    EvaluateMetaCommand(text);
                else
                    EvaluateSubmission(text);

                _submissionHistory.Add(text);
                _submissionHistoryIndex = _submissionHistory.Count;
            }
        }

        private sealed class SubmissionView
        {
            private readonly Action<string> _lineRenderer;
            private readonly ObservableCollection<string> _submissionDocument;
            private readonly int _cursorTop;
            private int _renderedLineCount;
            private int _currentLine;
            private int _currentCharacter;

            public SubmissionView(Action<string> lineRenderer, ObservableCollection<string> submissionDocument)
            {
                _lineRenderer = lineRenderer;
                _submissionDocument = submissionDocument;
                _submissionDocument.CollectionChanged += SubmissionDocumentChanged;
                _cursorTop = Console.CursorTop;
                Render();
            }

            private void SubmissionDocumentChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                Render();
            }

            private void Render()
            {
                Console.CursorVisible = false;

                var lineCount = 0;

                foreach (var line in _submissionDocument)
                {
                    Console.SetCursorPosition(0, _cursorTop + lineCount);
                    Console.ForegroundColor = ConsoleColor.Green;

                    Console.Write(lineCount == 0 ? "» " : "· ");

                    Console.ResetColor();
                    _lineRenderer(line);
                    Console.WriteLine(new string(' ', Console.WindowWidth - line.Length));
                    lineCount++;
                }

                var numberOfBlankLines = _renderedLineCount - lineCount;
                if (numberOfBlankLines > 0)
                {
                    var blankLine = new string(' ', Console.WindowWidth);
                    for (var i = 0; i < numberOfBlankLines; i++)
                    {
                        Console.SetCursorPosition(0, _cursorTop + lineCount + i);
                        Console.WriteLine(blankLine);
                    }
                }

                _renderedLineCount = lineCount;

                Console.CursorVisible = true;
                UpdateCursorPosition();
            }

            private void UpdateCursorPosition()
            {
                Console.CursorTop = _cursorTop + _currentLine;
                Console.CursorLeft = 2 + _currentCharacter;
            }

            public int CurrentLine
            {
                get => _currentLine;
                set
                {
                    if (_currentLine != value)
                    {
                        _currentLine = value;
                        _currentCharacter = Math.Min(_submissionDocument[_currentLine].Length, _currentCharacter);

                        UpdateCursorPosition();
                    }
                }
            }

            public int CurrentCharacter
            {
                get => _currentCharacter;
                set
                {
                    if (_currentCharacter != value)
                    {
                        _currentCharacter = value;
                        UpdateCursorPosition();
                    }
                }
            }
        }

        private string EditSubmission()
        {
            _done = false;

            var document = new ObservableCollection<string>() { "" };
            var view = new SubmissionView(RenderLine, document);

            while (!_done)
            {
                var key = Console.ReadKey(true);
                HandleKey(key, document, view);
            }

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;
            Console.WriteLine();

            return string.Join(Environment.NewLine, document);
        }

        private void HandleKey(ConsoleKeyInfo key, ObservableCollection<string> document, SubmissionView view)
        {
            if (key.Modifiers == default(ConsoleModifiers))
            {
                switch (key.Key)
                {
                    case ConsoleKey.Escape:
                        HandleEscape(document, view);
                        break;

                    case ConsoleKey.Enter:
                        HandleEnter(document, view);
                        break;

                    case ConsoleKey.LeftArrow:
                        HandleLeftArrow(document, view);
                        break;

                    case ConsoleKey.RightArrow:
                        HandleRightArrow(document, view);
                        break;

                    case ConsoleKey.UpArrow:
                        HandleUpArrow(document, view);
                        break;

                    case ConsoleKey.DownArrow:
                        HandleDownArrow(document, view);
                        break;

                    case ConsoleKey.Backspace:
                        HandleBackspace(document, view);
                        break;

                    case ConsoleKey.Delete:
                        HandleDelete(document, view);
                        break;

                    case ConsoleKey.Home:
                        HandleHome(document, view);
                        break;

                    case ConsoleKey.End:
                        HandleEnd(document, view);
                        break;

                    case ConsoleKey.Tab:
                        HandleTab(document, view);
                        break;

                    case ConsoleKey.PageUp:
                        HandlePageUp(document, view);
                        break;

                    case ConsoleKey.PageDown:
                        HandlePageDown(document, view);
                        break;
                }
            }
            else if (key.Modifiers == ConsoleModifiers.Control)
            {
                switch (key.Key)
                {
                    case ConsoleKey.Enter:
                        HandleControlEnter(document, view);
                        break;
                }
            }

            if (key.KeyChar >= ' ')
                HandleTyping(document, view, key.KeyChar.ToString());
        }

        private void HandleEscape(ObservableCollection<string> document, SubmissionView view)
        {
            document.Clear();
            document.Add(string.Empty);
            view.CurrentCharacter = 0;
            view.CurrentLine = 0;
        }

        private void HandleEnter(ObservableCollection<string> document, SubmissionView view)
        {
            var submissionText = string.Join(Environment.NewLine, document);
            if (submissionText.StartsWith("#") || IsCompleteSubmission(submissionText))
            {
                _done = true;
                return;
            }

            InsertLine(document, view);
        }

        private void HandleControlEnter(ObservableCollection<string> document, SubmissionView view)
        {
            InsertLine(document, view);
        }

        private static void InsertLine(ObservableCollection<string> document, SubmissionView view)
        {
            var remainder = document[view.CurrentLine].Substring(view.CurrentCharacter);
            document[view.CurrentLine] = document[view.CurrentLine].Substring(0, view.CurrentCharacter);

            var lineIndex = view.CurrentLine + 1;
            document.Insert(lineIndex, remainder);
            view.CurrentCharacter = 0;
            view.CurrentLine = lineIndex;
        }

        private void HandleLeftArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentCharacter > 0)
                view.CurrentCharacter--;
        }

        private void HandleRightArrow(ObservableCollection<string> document, SubmissionView view)
        {
            var line = document[view.CurrentLine];
            if (view.CurrentCharacter <= line.Length - 1)
                view.CurrentCharacter++;
        }

        private void HandleUpArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine > 0)
                view.CurrentLine--;
        }

        private void HandleDownArrow(ObservableCollection<string> document, SubmissionView view)
        {
            if (view.CurrentLine < document.Count - 1)
                view.CurrentLine++;
        }

        private void HandleBackspace(ObservableCollection<string> document, SubmissionView view)
        {
            var start = view.CurrentCharacter;
            if (start == 0)
            {
                if (view.CurrentLine == 0)
                    return;

                var currentLine = document[view.CurrentLine];
                var previousLine = document[view.CurrentLine - 1];
                document.RemoveAt(view.CurrentLine);
                view.CurrentLine--;
                document[view.CurrentLine] = previousLine + currentLine;
                view.CurrentCharacter = previousLine.Length;
                return;
            }
            else
            {
                var lineIndex = view.CurrentLine;
                var line = document[lineIndex];
                var before = line.Substring(0, start - 1);
                var after = line.Substring(start);
                document[lineIndex] = before + after;
                view.CurrentCharacter--;
            }
        }

        private void HandleDelete(ObservableCollection<string> document, SubmissionView view)
        {
            var lineIndex = view.CurrentLine;
            var line = document[lineIndex];
            var start = view.CurrentCharacter;
            if (start >= line.Length)
                return;

            var before = line.Substring(0, start);
            var after = line.Substring(start + 1);
            document[lineIndex] = before + after;
        }

        private void HandleHome(ObservableCollection<string> document, SubmissionView view)
        {
            view.CurrentCharacter = 0;
        }

        private void HandleEnd(ObservableCollection<string> document, SubmissionView view)
        {
            view.CurrentCharacter = document[view.CurrentLine].Length;
        }

        private void HandleTab(ObservableCollection<string> document, SubmissionView view)
        {
            const int TabWidth = 4;
            var start = view.CurrentCharacter;
            var remainingSpaces = TabWidth - start % TabWidth;
            var line = document[view.CurrentLine];
            document[view.CurrentLine] = line.Insert(start, new string(' ', remainingSpaces));
            view.CurrentCharacter += remainingSpaces;
        }

        private void HandlePageUp(ObservableCollection<string> document, SubmissionView view)
        {
            _submissionHistoryIndex = Math.Max(_submissionHistoryIndex - 1, 0);
            UpdateDocumentFromHistory(document, view);
        }

        private void HandlePageDown(ObservableCollection<string> document, SubmissionView view)
        {
            _submissionHistoryIndex = Math.Min(_submissionHistoryIndex + 1, _submissionHistory.Count);
            UpdateDocumentFromHistory(document, view);
        }

        private void UpdateDocumentFromHistory(ObservableCollection<string> document, SubmissionView view)
        {
            document.Clear();
            var historyItem = _submissionHistoryIndex == _submissionHistory.Count ? "" : _submissionHistory[_submissionHistoryIndex];
            var lines = historyItem.Split(Environment.NewLine);
            foreach (var line in lines)
                document.Add(line);

            view.CurrentLine = document.Count - 1;
            view.CurrentCharacter = document[view.CurrentLine].Length;
        }

        private void HandleTyping(ObservableCollection<string> document, SubmissionView view, string text)
        {
            var lineIndex = view.CurrentLine;
            var start = view.CurrentCharacter;
            document[lineIndex] = document[lineIndex].Insert(start, text);
            view.CurrentCharacter += text.Length;
        }

        protected void ClearHistory()
        {
            _submissionHistory.Clear();
        }

        protected virtual void RenderLine(string line)
        {
            Console.Write(line);
        }

        private void EvaluateMetaCommand(string input)
        {
            var lexer = new ArgumentLexer(input.Substring(1));
            var args = new List<string>();

            while (true)
            {
                var arg = lexer.NextArgument();
                if (arg == null)
                    break;

                args.Add(arg);
            }

            var diags = lexer.Diagnostics.ToArray();
            if (diags.Any())
            {
                foreach (var diag in diags)
                    WriteError(diag);

                return;
            }

            if (args.Count == 0)
            {
                WriteError("Invalid command");
            }

            var commandName = args[0];
            args.RemoveAt(0);
            var command = _metaCommands.SingleOrDefault(mc => mc.Name == commandName);
            if (command == null)
            {
                WriteError($"Invalid command {input}.");
                return;
            }

            var metaParams = command.Method.GetParameters();
            if (metaParams.Length != args.Count)
            {
                var paramsString = string.Join(" ", metaParams.Select(p => $"<{p.Name}>"));
                WriteError($"error: invalid number of arguments for command {args[0]}");
                WriteError($"usage: #{args[0]} {paramsString}");
                return;
            }

            command.Method.Invoke(this, args.Select(x => (object)x).ToArray());
        }

        protected void WriteError(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(text);
            Console.ResetColor();
        }

        protected abstract bool IsCompleteSubmission(string text);

        protected abstract void EvaluateSubmission(string text);

        [MetaCommand("help", "Display this help")]
        protected void MetaHelp()
        {
            var maxNameLength = _metaCommands.Max(x => x.Name.Length);
            foreach (var metaCommand in _metaCommands.OrderBy(cmd => cmd.Name))
            {
                var paddedName = metaCommand.Name.PadRight(maxNameLength);

                Console.Out.WritePunctuation("#");
                Console.Out.WriteIdentifier(paddedName);
                Console.Out.WritePunctuation($"  {metaCommand.Description}");
                Console.Out.WriteLine();
            }
        }

        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        protected sealed class MetaCommandAttribute : Attribute
        {
            public string Name { get; }
            public string Description { get; }

            public MetaCommandAttribute(string name, string description)
            {
                Name = name;
                Description = description;
            }
        }

        private sealed class MetaCommand
        {
            public string Name { get; }
            public string Description { get; }
            public MethodInfo Method { get; }

            public MetaCommand(string name, string description, MethodInfo method)
            {
                Name = name;
                Description = description;
                Method = method;
            }
        }

        internal class ArgumentLexer
        {
            private readonly string _text;
            private readonly List<string> _diagnostics = new List<string>();
            private int _position;

            public ArgumentLexer(string text)
            {
                _text = text;
            }

            public IEnumerable<string> Diagnostics => _diagnostics.ToArray();

            private char Current => Peek(_position);
            private char Lookahead => Peek(_position + 1);

            private char Peek(int position) => position >= _text.Length ? '\0' : _text[position];

            private void Next()
            {
                _position++;
            }

            private bool IfWhile(Func<char, bool> predicate)
            {
                if (!predicate(Current))
                    return false;

                while (predicate(Current))
                {
                    Next();
                }

                return true;
            }

            public string? NextArgument()
            {
                while (true)
                {
                    var start = _position;

                    switch (Current)
                    {
                        case '\0':
                            return null;

                        case '"':
                            return ParseStringToken(start);

                        default:
                            if (IfWhile(char.IsWhiteSpace))
                                continue;

                            while (Current != '"' && Current != '\0' && !char.IsWhiteSpace(Current))
                            {
                                Next();
                            }

                            return _text[start.._position];
                    }
                }
            }

            private string? ParseStringToken(int start)
            {
                Next(); // start "
                var sb = new StringBuilder();
                while (true)
                {
                    switch (Current)
                    {
                        case '"':
                            Next(); // end "
                            break;

                        case '\\': // escape sequence
                            var escapeSequence = ParseEscapeSequence();
                            if (escapeSequence != null)
                                sb.Append(escapeSequence);

                            continue;
                        case '\n':
                        case '\r':
                        case '\0':
                            _diagnostics.Add($"Unterminated string at {start}");
                            break;

                        default:
                            sb.Append(Current);
                            Next();
                            continue;
                    }

                    break;
                }

                return sb.ToString();
            }

            private string? ParseEscapeSequence()
            {
                var escapeStart = _position;
                Next(); // accept \
                switch (Current)
                {
                    case 'r':
                        Next();
                        return "\r";

                    case 'n':
                        Next();
                        return "\n";

                    case 't':
                        Next();
                        return "\t";

                    case '\\':
                        Next();
                        return "\\";

                    case '"':
                        Next();
                        return "\"";

                    case 'u':
                        Next(); //u
                        return ParseUtfEscapeSequence(4, escapeStart);

                    case 'U':
                        Next(); //U
                        return ParseUtfEscapeSequence(8, escapeStart);

                    default:
                        _diagnostics.Add($"Invalid escape sequence at {escapeStart}");
                        return null;
                }
            }

            private string? ParseUtfEscapeSequence(int digits, int escapeStart)
            {
                var value = 0;
                for (var i = 0; i < digits; i++)
                {
                    if (!HexValue(out var hexValue))
                    {
                        _diagnostics.Add($"Invalid escape sequence at {escapeStart}");
                        return null;
                    }

                    value += hexValue << 4 * (digits - 1 - i);
                    Next();
                }

                return ((char)value).ToString();
            }

            private bool HexValue(out int value)
            {
                try
                {
                    value = int.Parse(Current.ToString(), System.Globalization.NumberStyles.HexNumber);
                    return true;
                }
                catch
                {
                    value = 0;
                    return false;
                }
            }
        }
    }
}