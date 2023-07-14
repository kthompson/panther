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
            $@".class Program {{
                 .method main 0 (): void {{
                     ldloc
                     [][nop]
                     ldstr taco
                 }}
               }}
            ";

        var diagnostic =
            @"
                Expected end of line trivia but none found
                Unexpected token nop, expected number
            ";

        AssertHasDiagnostics(text, diagnostic);
    }

    [Fact]
    public void ReportEndOfLine()
    {
        var text =
            $@".class Program {{
                 .method main 0 (): void {{
                    [nop] taco
                 }}
               }}
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
               .class Program
                 .method main
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
