extern alias TestLib;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FsCheck;
using FsCheck.Xunit;
using Moq;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Symbols;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis;

// NOTE: We bring in two different versions of the Panther Standard
// Library one being the Actual StdLib and one being a Test version
// With the Extern alias we can access the types from the specific
// library we want
extern alias StdLib;

[Properties(MaxTest = 10)]
public class EvaluationTests
{
    [Property]
    public void EvaluatesNumbers(int number)
    {
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(number.ToString(), number, scriptHost);
    }

    [Fact]
    public void EvaluatesPrefixExpressionWithLineBreak()
    {
        using var scriptHost = BuildScriptHost();
        AssertEvaluation("7 +\n4", 11, scriptHost);
    }

    [Fact]
    public void EvaluatesHelloWorld()
    {
        using var scriptHost = BuildScriptHostTestLib();

        // mock readLine
        Execute($"mockReadLine(\"Kevin\")", scriptHost);

        AssertEvaluation(
            @"{
                                   println(""What is your name?"")
                                   val name = readLine()
                                   val message = ""Hello, "" + name
                                   println(message)
                                   message
                               }",
            "Hello, Kevin",
            scriptHost
        );

        var expectedOutput = BuildExpectedOutput("What is your name?", "Hello, Kevin");
        // verify mock
        AssertEvaluation(@"getOutput()", expectedOutput, scriptHost);
    }

    [Fact(Skip = "TODO")]
    public void EvaluatesStringEqualityWithConstructedString()
    {
        using var scriptHost = BuildScriptHostTestLib();

        // TODO: currently the problem here is that we end up using ceq for string
        // equality when instead we should be using the string's `==` operator
        // some cases do pass though when we have strings that are constants
        // on both sides of the equality check
        Execute(@$"def stringTest(arg: string) = ""hi"" + arg", scriptHost);

        AssertEvaluation(@"stringTest(""gh"") == ""high""", true, scriptHost);
    }

    [Property]
    public void EvaluatesAddition(int number, int number2)
    {
        var code = $"{number} + {number2}";
        object value = number + number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(20)]
    [InlineData(37)]
    public void EvaluatesMutualRecursion(int number)
    {
        var code =
            $@"
            even({number})

            def even(number: int): bool = if(number == 0) true else odd(number - 1)
            def odd(number: int): bool = if(number == 0) false else even(number - 1)
            ";
        object value = (number % 2) == 0;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Theory]
    [InlineData("\"\"", "")]
    [InlineData("\"\\u1a2d\"", "\u1a2d")]
    [InlineData("\"\\t\"", "\t")]
    [InlineData("\"\\\\\"", "\\")]
    [InlineData("\"\\ud83d\\ude02\"", "😂")]
    public void EvaluatesEscapeSequences(string code, string expected)
    {
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, expected, scriptHost);
    }

    [Property]
    public void EvaluatesIntToStringConversion(int number)
    {
        var code = $"string({number})";
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, number.ToString(), scriptHost);
    }

    [Property]
    public void EvaluatesBoolToStringConversion(bool value)
    {
        var code = $"string({b(value)})";
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value.ToString(), scriptHost);
    }

    [Property]
    public void EvaluatesStringConcatenation(NonNull<string> str1, NonNull<string> str2)
    {
        var code = $"{escapeString(str1)} + {escapeString(str2)}";
        var expected = str1.Item + str2.Item;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, expected, scriptHost);
    }

    [Property]
    public void EvaluatesSubtraction(int number, int number2)
    {
        var code = $"{number} - {number2}";
        object value = number - number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesMultiplication(int number, int number2)
    {
        var code = $"{number} * {number2}";
        object value = number * number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesLessThan(int number, int number2)
    {
        var code = $"{number} < {number2}";
        object value = number < number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesGreaterThan(int number, int number2)
    {
        var code = $"{number} > {number2}";
        object value = number > number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesLessThanOrEqual(int number, int number2)
    {
        var code = $"{number} <= {number2}";
        object value = number <= number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesGreaterThanOrEqual(int number, int number2)
    {
        var code = $"{number} >= {number2}";
        object value = number >= number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesNegation(int number)
    {
        var code = $"-{number}";
        object value = -number;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesPlus(int number)
    {
        var code = $"+{number}";
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, number, scriptHost);
    }

    [Property]
    public void EvaluatesBitwiseNegation(int number)
    {
        var code = $"~{number}";
        object value = ~number;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesLogicalNegation(bool value)
    {
        var code = $"!{b(value)}";
        object value1 = !value;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value1, scriptHost);
    }

    [Property]
    public void EvaluatesBitwiseAnd(int number, int number2)
    {
        var code = $"{number} & {number2}";
        object value = number & number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesBitwiseOr(int number, int number2)
    {
        var code = $"{number} | {number2}";
        object value = number | number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesBitwiseXor(int number, int number2)
    {
        var code = $"{number} ^ {number2}";
        object value = number ^ number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesIf(bool condition, int number, int number2)
    {
        var code = $"if ({b(condition)}) {number} else {number2}";
        object value = condition ? number : number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesMultiLineIf(bool condition, int number, int number2)
    {
        var code =
            $@"if ({b(condition)})
                                {number}
                                else {number2}";
        object value = condition ? number : number2;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesNestedIf(bool condition, bool condition2, int number)
    {
        var code =
            $@"if ({b(condition)})
                                {number}
                                else if ({b(condition2)}) 5 else 1";
        object value = condition
            ? number
            : condition2
                ? 5
                : 1;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesNestedIfBinding(bool conditionA, bool conditionB)
    {
        var code =
            $@"if ({b(conditionA)})
                                if ({b(conditionB)}) 1 else 2
                                else 5";
        object value = conditionA ? (conditionB ? 1 : 2) : 5;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesNestedIfBinding2(bool conditionA, bool conditionB)
    {
        var code =
            $@"if ({b(conditionA)})
                                2
                                else if ({b(conditionB)}) 1 else 5";
        object value = conditionA ? 2 : (conditionB ? 1 : 5);
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesDivision(int number, NonZeroInt number2)
    {
        using var scriptHost = BuildScriptHost();
        AssertEvaluation($"{number} / {number2}", number / number2.Item, scriptHost);
    }

    [Property]
    public void EvaluatesAssignment(int number)
    {
        var code =
            $@"{{
                                    var x = {number}
                                    x = 1
                                }}";
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, StdLib::Panther.Unit.Default, scriptHost);
    }

    [Property]
    public void EvaluatesNestedAssignment(int number)
    {
        var code =
            $@"{{
                                    var x = 0
                                    val y = x = {number}
                                    x
                                }}";
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, number, scriptHost);
    }

    [Property]
    public void EvaluatesWhile(PositiveInt number)
    {
        var code =
            $@"{{
                                    var times = {number.Item}
                                    var count = 0
                                    while (times > 0) {{
                                       count = count + 1
                                       times = times - 1
                                    }}
                                    count
                                }}";
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, number.Item, scriptHost);
    }

    [Property]
    public void EvaluatesFor(int from, int to)
    {
        var result = 0;
        for (var i = from; i < to; i++)
        {
            result += i;
        }

        var code =
            $@"{{
                                    var count = 0
                                    for (x <- {@from} to {to}) count = count + x
                                    count
                                }}";
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, result, scriptHost);
    }

    [Property]
    public void EvaluatesAnd(bool left, bool right)
    {
        using var scriptHost = BuildScriptHost();
        AssertEvaluation($"{b(left)} && {b(right)}", left && right, scriptHost);
    }

    [Property]
    public void EvaluatesBooleanLiteral(bool literal)
    {
        var code = $"{b(literal)}";
        object value = literal;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesOr(bool left, bool right)
    {
        var code = $"{b(left)} || {b(right)}";
        object value = left || right;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesBoolEquality(bool left, bool right)
    {
        var code = $"{b(left)} == {b(right)}";
        object value = left == right;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesIntEquality(int left, int right)
    {
        var code = $"{left} == {right}";
        object value = left == right;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesStringEquality(NonNull<string> left, NonNull<string> right)
    {
        var code = $"{escapeString(left)} == {escapeString(right)}";
        object value = left.Item == right.Item;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesStringInequality(NonNull<string> left, NonNull<string> right)
    {
        var code = $"{escapeString(left)} != {escapeString(right)}";
        object value = left.Item != right.Item;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesBoolInequality(bool left, bool right)
    {
        var code = $"{b(left)} != {b(right)}";
        object value = left != right;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesIntInequality(int left, int right)
    {
        var code = $"{left} != {right}";
        object value = left != right;
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, value, scriptHost);
    }

    [Property]
    public void EvaluatesParens(int number)
    {
        var code = $"({number})";
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, number, scriptHost);
    }

    [Property]
    public void EvaluatesBooleans(bool b)
    {
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(b.ToString().ToLower(), b, scriptHost);
    }

    [Property]
    public void EvaluatesBoundInt(int n)
    {
        using var scriptHost = BuildScriptHost();
        Execute($"val a = {n}", scriptHost);

        AssertEvaluation($"a", n, scriptHost);
    }

    [Property]
    public void EvaluatesMethodContinuation(int n)
    {
        using var scriptHost = BuildScriptHost();
        Execute($"def a() = {n}", scriptHost);

        AssertEvaluation($"a()", n, scriptHost);
    }

    [Property]
    public void EvaluatesBoundBool(bool n)
    {
        using var scriptHost = BuildScriptHost();
        Execute($"val a = {n.ToString().ToLower()}", scriptHost);

        AssertEvaluation("a", n, scriptHost);
    }

    [Property]
    public void EvaluatesIndexExpression(NonEmptyString str)
    {
        var code = $"{escapeString(str.Item)}[0]";
        var expected = str.Item[0];
        using var scriptHost = BuildScriptHost();
        AssertEvaluation(code, expected, scriptHost);
    }

    string escapeString(NonNull<string> str) => escapeString(str.Item);

    static string escapeString(string strItem)
    {
        var sb = new StringBuilder();
        sb.Append('"');
        foreach (var c in strItem)
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
}
