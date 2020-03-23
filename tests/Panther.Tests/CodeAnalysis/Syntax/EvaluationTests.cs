using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;
using Xunit;

namespace Panther.Tests.CodeAnalysis.Syntax
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
            var code = $"val a = {n}";

            var dictionary = AssertEvaluation(code, n);

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
            var code = $"val a = {n.ToString().ToLower()}";

            var dictionary = AssertEvaluation(code, n);

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
            var dictionary = AssertEvaluation($"val a = {n}", n);

            AssertEvaluation($"a", n, dictionary);
        }

        [Property]
        public void EvaluatesBoundBool(bool n)
        {
            var dictionary = AssertEvaluation($"val a = {n.ToString().ToLower()}", n);

            AssertEvaluation($"a", n, dictionary);
        }

        private static Dictionary<VariableSymbol, object> AssertEvaluation(string code, object value, Dictionary<VariableSymbol, object> dictionary = null)
        {
            dictionary ??= new Dictionary<VariableSymbol, object>();
            var tree = SyntaxTree.Parse(code);
            var compilation = new Compilation(tree);

            var result = compilation.Evaluate(dictionary);

            Assert.NotNull(result);
            Assert.Empty(result.Diagnostics);
            Assert.Equal(value, result.Value);
            return dictionary;
        }
    }
}