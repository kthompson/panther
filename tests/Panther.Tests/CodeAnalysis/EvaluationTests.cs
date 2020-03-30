using System.Collections.Generic;
using System.Text;
using FsCheck;
using FsCheck.Xunit;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Symbols;
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

        [Fact]
        public void EvaluatesPrefixExpressionWithLineBreak()
        {
            AssertEvaluation("7 +\n4", 11);
        }

        [Property]
        public void EvaluatesAddition(int number, int number2)
        {
            AssertEvaluation($"{number} + {number2}", number + number2);
        }

        [Theory]
        [InlineData("\"\"", "")]
        [InlineData("\"\\u1a2d\"", "\u1a2d")]
        [InlineData("\"\\t\"", "\t")]
        [InlineData("\"\\\\\"", "\\")]
        [InlineData("\"\\ud83d\\ude02\"", "😂")]
        public void EvaluatesEscapeSequences(string code, string expected)
        {
            AssertEvaluation(code, expected);
        }

        [Property]
        public void EvaluatesStringConcatenation(NonNull<string> str1, NonNull<string> str2)
        {
            string escapeString(NonNull<string> str)
            {
                var sb = new StringBuilder();
                sb.Append('"');
                foreach (var c in str.Item)
                {
                    switch (c)
                    {
                        case '"':
                            sb.Append("\\\"");
                            break;

                        case '\n':
                            sb.Append("\\n");
                            break;

                        case '\r':
                            sb.Append("\\r");
                            break;

                        case '\t':
                            sb.Append("\\t");
                            break;

                        case '\\':
                            sb.Append("\\\\");
                            break;

                        default:
                            if (char.IsControl(c))
                            {
                                var value = (int)c;
                                if ((value & 0xffff0000) > 0)
                                {
                                    sb.Append("\\U");
                                    sb.Append(value.ToString("x8"));
                                }
                                else
                                {
                                    sb.Append("\\u");
                                    sb.Append(value.ToString("x4"));
                                }
                            }
                            else
                            {
                                sb.Append(c);
                            }

                            break;
                    }
                }

                sb.Append('"');
                return sb.ToString();
            }

            var code = $"{escapeString(str1)} + {escapeString(str2)}";
            var expected = str1.Item + str2.Item;
            AssertEvaluation(code, expected);
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
        public void EvaluatesNegation(int number)
        {
            AssertEvaluation($"-{number}", -number);
        }

        [Property]
        public void EvaluatesPlus(int number)
        {
            AssertEvaluation($"+{number}", number);
        }

        [Property]
        public void EvaluatesBitwiseNegation(int number)
        {
            AssertEvaluation($"~{number}", ~number);
        }

        [Property]
        public void EvaluatesNegate(bool value)
        {
            AssertEvaluation($"!{b(value)}", !value);
        }

        [Property]
        public void EvaluatesBitwiseAnd(int number, int number2)
        {
            AssertEvaluation($"{number} & {number2}", number & number2);
        }

        [Property]
        public void EvaluatesBitwiseOr(int number, int number2)
        {
            AssertEvaluation($"{number} | {number2}", number | number2);
        }

        [Property]
        public void EvaluatesBitwiseXor(int number, int number2)
        {
            AssertEvaluation($"{number} ^ {number2}", number ^ number2);
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
        public void EvaluatesNestedIf(bool condition, bool condition2, int number)
        {
            AssertEvaluation($@"if ({b(condition)})
                                {number}
                                else if ({b(condition2)}) 5 else 1", condition ? number : condition2 ? 5 : 1);
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
        public void EvaluatesAssignment(int number)
        {
            AssertEvaluation($@"{{
                                    var x = {number}
                                    x = 1
                                }}", Unit.Default);
        }

        [Property]
        public void EvaluatesNestedAssignment(int number)
        {
            AssertEvaluation($@"{{
                                    var x = 0
                                    val y = x = {number}
                                    x
                                }}", number);
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
        public void EvaluatesFor(int from, int to)
        {
            var result = 0;
            for (var i = from; i < to; i++)
            {
                result += i;
            }

            AssertEvaluation($@"{{
                                    var count = 0
                                    for (x <- {from} to {to}) count = count + x
                                    count
                                }}", result);
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
                Assert.Equal(TypeSymbol.Int, pair.Key.Type);
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
                Assert.Equal(TypeSymbol.Bool, pair.Key.Type);
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