using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis
{
    public class EvaluationTests
    {
        [Property]
        public void EvaluatesNumbers(int number)
        {
            AssertEvaluation(number.ToString(), number);
        }

        [Property]
        public void EvaluatesAddition(int number, int number2)
        {
            AssertEvaluation($"{number} + {number2}", number + number2);
        }

        [Property]
        public void EvaluatesSubtraction(int number, int number2)
        {
            AssertEvaluation($"{number} - {number2}", number - number2);
        }

        [Property]
        public void EvaluatesMultiplication(int number, int number2)
        {
            AssertEvaluation($"{number} * {number2}", number * number2);
        }

        [Property]
        public void EvaluatesDivision(int number, NonZeroInt number2)
        {
            AssertEvaluation($"{number} / {number2}", number / number2.Item);
        }

        [Property]
        public void EvaluatesAnd(bool left, bool right)
        {
            AssertEvaluation($"{left.ToString().ToLower()} && {right.ToString().ToLower()}", left && right);
        }

        [Property]
        public void EvaluatesOr(bool left, bool right)
        {
            AssertEvaluation($"{left.ToString().ToLower()} || {right.ToString().ToLower()}", left || right);
        }

        [Property]
        public void EvaluatesBoolEquality(bool left, bool right)
        {
            AssertEvaluation($"{left.ToString().ToLower()} == {right.ToString().ToLower()}", left == right);
        }

        [Property]
        public void EvaluatesIntEquality(int left, int right)
        {
            AssertEvaluation($"{left} == {right}", left == right);
        }

        [Property]
        public void EvaluatesBoolInequality(bool left, bool right)
        {
            AssertEvaluation($"{left.ToString().ToLower()} != {right.ToString().ToLower()}", left != right);
        }

        [Property]
        public void EvaluatesIntInequality(int left, int right)
        {
            AssertEvaluation($"{left} != {right}", left != right);
        }

        [Property]
        public void EvaluatesParens(int number)
        {
            AssertEvaluation($"({number})", number);
        }

        [Property]
        public void EvaluatesBooleans(bool b)
        {
            AssertEvaluation(b.ToString().ToLower(), b);
        }

        [Property]
        public void EvaluatesLogicalNegation(bool b)
        {
            AssertEvaluation("!" + b.ToString().ToLower(), !b);
        }

        [Property]
        public void EvaluatesValIntCreation(int n)
        {
            Dictionary<VariableSymbol, object> dictionary = null;

            Compile($"val a = {n}", ref dictionary, null, out _);

            Assert.Collection(dictionary, pair =>
            {
                Assert.Equal("a", pair.Key.Name);
                Assert.Equal(typeof(int), pair.Key.Type);
                Assert.Equal(n, pair.Value);
            });
        }

        [Property]
        public void EvaluatesValBoolCreation(bool n)
        {
            Dictionary<VariableSymbol, object> dictionary = null;

            Compile($"val a = {n.ToString().ToLower()}", ref dictionary, null, out _);

            Assert.Collection(dictionary, pair =>
            {
                Assert.Equal("a", pair.Key.Name);
                Assert.Equal(typeof(bool), pair.Key.Type);
                Assert.Equal(n, pair.Value);
            });
        }

        [Property]
        public void EvaluatesBoundInt(int n)
        {
            Dictionary<VariableSymbol, object> dictionary = null;
            var compilation = Compile($"val a = {n}", ref dictionary, null, out _);

            AssertEvaluation($"a", n, dictionary, compilation);
        }

        [Property]
        public void EvaluatesBoundBool(bool n)
        {
            Dictionary<VariableSymbol, object> dictionary = null;
            var compilation = Compile($"val a = {n.ToString().ToLower()}", ref dictionary, null, out _);

            AssertEvaluation($"a", n, dictionary, compilation);
        }

        [Property]
        public void ReportUndefinedBinaryOperatorForMixedTypes(int left, bool right)
        {
            var text = $@"
                {{
                    val x = {left}
                    val y = {b(right)}

                    x [+] y
                }}
            ";

            var diagnostic = @"
                Binary operator '+' is not defined for types System.Int32 and System.Boolean
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Property]
        public void ReportUndefinedUnaryOperatorForIntType(int left)
        {
            var text = $@"
                {{
                    val x = {left}

                    [!]x
                }}
            ";

            var diagnostic = @"
                Unary operator '!' is not defined for type System.Int32
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Property]
        public void ReportUndefinedUnaryOperatorForBoolType(bool value)
        {
            var text = $@"
                {{
                    val x = {b(value)}

                    [-]x
                    [+]x
                }}
            ";

            var diagnostic = @"
                Unary operator '-' is not defined for type System.Boolean
                Unary operator '+' is not defined for type System.Boolean
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportUndefinedNames()
        {
            var text = @"[x] + [y]";

            var diagnostic = @"
                Variable 'x' does not exist
                Variable 'y' does not exist
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        private string b(bool value) => value ? "true" : "false";

        private void AssertHasDiagnostics(string text, string diagnosticText)
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

        private static void AssertEvaluation(string code, object value,
            Dictionary<VariableSymbol, object> dictionary = null, Compilation previous = null)
        {
            Compile(code, ref dictionary, previous, out var result);
            Assert.Equal(value, result.Value);
        }

        private static Compilation Compile(string code, ref Dictionary<VariableSymbol, object> dictionary, Compilation previous,
            out EvaluationResult result)
        {
            dictionary ??= new Dictionary<VariableSymbol, object>();
            var tree = SyntaxTree.Parse(code);
            var compilation = previous == null ? new Compilation(tree) : previous.ContinueWith(tree);

            result = compilation.Evaluate(dictionary);

            Assert.NotNull(result);
            Assert.Empty(result.Diagnostics);
            return compilation;
        }
    }
}