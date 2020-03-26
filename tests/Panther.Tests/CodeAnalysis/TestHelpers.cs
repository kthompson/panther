using System.Collections.Generic;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis
{
    public static class TestHelpers
    {
        public static string b(bool value) => value ? "true" : "false";

        public static void AssertHasDiagnostics(string text, string diagnosticText)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);
            var compilation = new Compilation(syntaxTree);
            var result = compilation.Evaluate(new Dictionary<VariableSymbol, object>());
            var diagnostics = AnnotatedText.UnindentLines(diagnosticText);

            Assert.True(annotatedText.Spans.Length == diagnostics.Length, "Test invalid, must have equal number of diagnostics as text spans");

            for (var i = 0; i < diagnostics.Length; i++)
            {
                Assert.True(result.Diagnostics.Length > i);

                var expectedMessage = diagnostics[i];
                var actualMessage = result.Diagnostics[i].Message;
                Assert.Equal(expectedMessage, actualMessage);

                var expectedSpan = annotatedText.Spans[i];
                var actualSpan = result.Diagnostics[i].Span;
                Assert.Equal(expectedSpan, actualSpan);
            }

            Assert.Equal(diagnostics.Length, result.Diagnostics.Length);
        }
    }
}