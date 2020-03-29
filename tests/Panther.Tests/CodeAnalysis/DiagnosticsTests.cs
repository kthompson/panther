using FsCheck.Xunit;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis
{
    public class DiagnosticsTests
    {
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
                Binary operator '+' is not defined for types 'int' and 'bool'
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
                Unary operator '!' is not defined for type 'int'
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
                Unary operator '-' is not defined for type 'bool'
                Unary operator '+' is not defined for type 'bool'
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void DontReportCascadingErrors()
        {
            var text = @"(true [*] 1) + 7";

            var diagnostic = @"
                Binary operator '*' is not defined for types 'bool' and 'int'
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

        [Fact]
        public void ReportIncompleteBlock()
        {
            var text = @"{
                            []";

            var diagnostic = @"
                Unexpected token EndOfInputToken, expected CloseBraceToken
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportIncompleteGroup()
        {
            var text = @"([][]";

            var diagnostic = @"
                Unexpected token EndOfInputToken, expected Expression
                Unexpected token EndOfInputToken, expected CloseParenToken
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportAlreadyDefinedVariable()
        {
            var text = @"{
                    val a = 1
                    val [a] = 2
                }";

            var diagnostic = @"
                Variable 'a' is already defined in the current scope
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportInvalidIfCondition()
        {
            var text = @"{
                    if ([5])
                    7
                    else 3
                }";

            var diagnostic = @"
                Type mismatch. Required 'bool', found 'int'
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportInvalidWhileCondition()
        {
            var text = @"while ([5 + 1]) 7";

            var diagnostic = @"
                Type mismatch. Required 'bool', found 'int'
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Property]
        public void ReportInvalidForLowerBound(bool lower)
        {
            var text = $@"for (x <- [{b(lower)}] to 12) 7";

            var diagnostic = @"
                Type mismatch. Required 'int', found 'bool'
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Property]
        public void ReportInvalidForUpperBound(bool upper)
        {
            var text = $@"for (x <- 7 to [{b(upper)}]) 7";

            var diagnostic = @"
                Type mismatch. Required 'int', found 'bool'
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportMismatchedBranches()
        {
            var text = @"{
                    if (true)
                    true
                    else [3]
                }";

            var diagnostic = @"
                Type mismatch. Required 'bool', found 'int'
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportMismatchedBranches2()
        {
            var text = @"{
                    if (true)
                    1
                    else [true]
                }";

            var diagnostic = @"
                Type mismatch. Required 'int', found 'bool'
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportAlreadyDefinedVariable2()
        {
            var text = @"{
                    val a = 1
                    var [a] = 2
                }";

            var diagnostic = @"
                Variable 'a' is already defined in the current scope
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportAlreadyDefinedVariable3()
        {
            var text = @"{
                    var a = 1
                    val [a] = 2
                }";

            var diagnostic = @"
                Variable 'a' is already defined in the current scope
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportUndefinedName()
        {
            var text = @"[a] = 1";

            var diagnostic = @"
                Variable 'a' does not exist
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportDeclarationInNewScope()
        {
            var text = @"{
                    val a = 1
                    {
                        val a = 2
                    }
                }";

            var diagnostic = @"";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportReassignmentToVal()
        {
            var text = @"{
                    val a = 1
                    [a] = 2
                }";

            var diagnostic = @"
                Reassignment to val 'a'
            ";

            AssertHasDiagnostics(text, diagnostic);
        }

        [Fact]
        public void ReportTypeMismatch()
        {
            var text = @"{
                    var a = 1
                    a = [true]
                }";

            var diagnostic = @"
                Type mismatch. Required 'int', found 'bool'
            ";

            AssertHasDiagnostics(text, diagnostic);
        }
    }
}