using System.Collections.Generic;
using System.Linq;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.StdLib;
using Xunit;

namespace Panther.Tests.CodeAnalysis
{
    public static class TestHelpers
    {
        public static string b(bool value) => value ? "true" : "false";

        private class TestBuiltins : IBuiltins
        {
            public string Read()
            {
                return "";
            }

            public void Print(string message)
            {
            }
        }

        public static void AssertHasDiagnostics(string text, string diagnosticText, IBuiltins? builtins = null)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);
            var compilation = Compilation.CreateScript(null, builtins ?? new TestBuiltins(), syntaxTree);
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());
            var expectedDiagnosticMessages = AnnotatedText.UnindentLines(diagnosticText);

            Assert.True(annotatedText.Spans.Length == expectedDiagnosticMessages.Length, "Test invalid, must have equal number of diagnostics as text spans");

            var expectedDiagnostics = expectedDiagnosticMessages.Zip(annotatedText.Spans)
                .Select(tuple => new { Span = tuple.Second, Message = tuple.First })
                .OrderBy(diagnostic => diagnostic.Span.Start)
                .ToArray();

            var actualDiagnostics = result.Diagnostics.OrderBy(diagnostic => diagnostic.Location.Span.Start).ToArray();

            for (var i = 0; i < expectedDiagnosticMessages.Length; i++)
            {
                Assert.True(expectedDiagnostics.Length > i, $"Expected more diagnostics ({expectedDiagnosticMessages.Length}) than actual {expectedDiagnostics.Length}");

                var expectedMessage = expectedDiagnostics[i].Message;
                var actualMessage = actualDiagnostics[i].Message;
                Assert.Equal(expectedMessage, actualMessage);

                var expectedSpan = expectedDiagnostics[i].Span;
                var actualSpan = actualDiagnostics[i].Span;
                Assert.Equal(expectedSpan, actualSpan);
            }

            Assert.Equal(expectedDiagnosticMessages.Length, expectedDiagnostics.Length);
        }

        public static void AssertEvaluation(string code, object value,
            Dictionary<VariableSymbol, object>? dictionary = null, Compilation? previous = null, IBuiltins? builtins = null)
        {
            Compile(code, ref dictionary, previous, builtins, out var result);
            Assert.Equal(value, result.Value);
        }

        public static Compilation Compile(string code, ref Dictionary<VariableSymbol, object>? dictionary) =>
            Compile(code, ref dictionary, null, null, out var result);

        public static Compilation Compile(string code, ref Dictionary<VariableSymbol, object>? dictionary,
            Compilation? previous,
            IBuiltins? builtins,
            out EvaluationResult result)
        {
            dictionary ??= new Dictionary<VariableSymbol, object>();
            var tree = SyntaxTree.Parse(code);
            Assert.Empty(tree.Diagnostics);
            var compilation = Compilation.CreateScript(previous, builtins ?? new TestBuiltins(), tree);

            result = compilation.Evaluate(dictionary);

            Assert.NotNull(result);
            Assert.Empty(result.Diagnostics);
            return compilation;
        }
    }
}