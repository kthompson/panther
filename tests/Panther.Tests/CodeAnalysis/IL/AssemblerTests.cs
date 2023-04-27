using System.Runtime.CompilerServices;
using Panther.CodeAnalysis.IL;
using Xunit;
using static Panther.Tests.CodeAnalysis.TestHelpers;

namespace Panther.Tests.CodeAnalysis.IL;

public class AssemblerTests
{
    [Fact]
    public void ReportIntOperandInstructions()
    {
        var text =
            $@"ldloc
               [nop]
               ldstr [taco]
            ";

        var diagnostic =
            @"
                Unexpected token nop, expected number
                Unexpected token identifier, expected string
            ";

        AssertHasDiagnostics(text, diagnostic);
    }

    [Fact]
    public void ReportEndOfLine()
    {
        var text =
            $@"[nop] taco
            ";

        var diagnostic =
            @"
                Expected end of line trivia but none found
            ";

        AssertHasDiagnostics(text, diagnostic);
    }

    [Fact]
    public void ParseFunctionInstructions()
    {
        var text =
            $@"
               function my_method 5
                  label taco_town
                  ldarg 0
                  ldarg 1
                  add
                  ldc 5
                  call my_method 2
            ";

        var diagnostic =
            @"
            ";

        AssertHasDiagnostics(text, diagnostic);
    }

    static void AssertHasDiagnostics(
        string text,
        string diagnosticText,
        [CallerMemberName] string? testName = null
    )
    {
        var annotatedText = AnnotatedText.Parse(text);

        var syntaxTree = Assembler.ParseText(annotatedText.Text);
        var diagnostics = syntaxTree.Diagnostics;

        AssertDiagnostics(diagnosticText, annotatedText, diagnostics);
    }
}
