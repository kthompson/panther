using System.Collections.Generic;
using FsCheck;
using FsCheck.Xunit;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Syntax;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

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
        public void EvaluatesLessThan(int number, int number2)
        {
            AssertEvaluation($"{number} < {number2}", number < number2);
        }

        [Property]
        public void EvaluatesGreaterThan(int number, int number2)
        {
            AssertEvaluation($"{number} > {number2}", number > number2);
        }

        [Property]
        public void EvaluatesLessThanOrEqual(int number, int number2)
        {
            AssertEvaluation($"{number} <= {number2}", number <= number2);
        }

        [Property]
        public void EvaluatesGreaterThanOrEqual(int number, int number2)
        {
            AssertEvaluation($"{number} >= {number2}", number >= number2);
        }

        [Property]
        public void EvaluatesIf(bool condition, int number, int number2)
        {
            AssertEvaluation($"if ({b(condition)}) {number} else {number2}", condition ? number : number2);
        }

        [Property]
        public void EvaluatesMultiLineIf(bool condition, int number, int number2)
        {
            AssertEvaluation($@"if ({b(condition)})
                                {number}
                                else {number2}", condition ? number : number2);
        }

        [Property]
        public void EvaluatesNestedIf(bool condition, int number)
        {
            AssertEvaluation($@"if ({b(condition)})
                                {number}
                                else if (true) 5 else 1", condition ? number : 5);
        }

        [Property]
        public void EvaluatesNestedIfBinding(bool conditionA, bool conditionB)
        {
            AssertEvaluation($@"if ({b(conditionA)})
                                if ({b(conditionB)}) 1 else 2
                                else 5", conditionA ? (conditionB ? 1 : 2) : 5);
        }

        [Property]
        public void EvaluatesNestedIfBinding2(bool conditionA, bool conditionB)
        {
            AssertEvaluation($@"if ({b(conditionA)})
                                2
                                else if ({b(conditionB)}) 1 else 5", conditionA ? 2 : (conditionB ? 1 : 5));
        }

        [Property]
        public void EvaluatesDivision(int number, NonZeroInt number2)
        {
            AssertEvaluation($"{number} / {number2}", number / number2.Item);
        }

        [Property]
        public void EvaluatesWhile(PositiveInt number)
        {
            AssertEvaluation($@"{{
                                    var times = {number.Item}
                                    var count = 0
                                    while (times > 0) {{
                                       count = count + 1
                                       times = times - 1
                                    }}
                                    count
                                }}", number.Item);
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
    }
}