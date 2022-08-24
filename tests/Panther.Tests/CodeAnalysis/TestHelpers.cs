extern alias TestLib;
extern alias StdLib;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Mono.Cecil;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Xunit;
using Xunit.Sdk;

namespace Panther.Tests.CodeAnalysis;

internal static partial class TestHelpers
{
    public static ImmutableArray<AssemblyDefinition> AssembliesWithStdLib { get; }
    public static ImmutableArray<AssemblyDefinition> AssembliesWithTestStdLib { get; }

    public static string b(bool value) => value ? "true" : "false";

    static TestHelpers()
    {
        AssembliesWithTestStdLib = GetAssemblyDefinitions(
            typeof(object).Assembly.Location,
            typeof(Console).Assembly.Location,
            typeof(TestLib::Panther.Unit).Assembly.Location
        );

        AssembliesWithStdLib = GetAssemblyDefinitions(
            typeof(object).Assembly.Location,
            typeof(Console).Assembly.Location,
            typeof(StdLib::Panther.Unit).Assembly.Location
        );
    }

    private static ImmutableArray<AssemblyDefinition> GetAssemblyDefinitions(
        params string[] references
    )
    {
        var assemblies = ImmutableArray.CreateBuilder<AssemblyDefinition>();

        foreach (var reference in references)
        {
            var asm = AssemblyDefinition.ReadAssembly(reference);
            assemblies.Add(asm);
        }

        var result = assemblies.ToImmutable();
        return result;
    }

    public static void AssertHasDiagnostics(
        string text,
        string diagnosticText,
        [CallerMemberName] string? testName = null
    )
    {
        var annotatedText = AnnotatedText.Parse(text);

        var syntaxTree = SyntaxTree.Parse(annotatedText.Text);
        using var scriptHost = new ScriptHost(AssembliesWithStdLib, testName ?? "test");

        var result = scriptHost.Execute(syntaxTree);

        var diagnostics = result.Diagnostics;

        AssertDiagnostics(diagnosticText, annotatedText, diagnostics);
    }

    public static void AssertDiagnostics(
        string diagnosticText,
        AnnotatedText annotatedText,
        ImmutableArray<Diagnostic> diagnostics
    )
    {
        var expectedDiagnosticMessages = diagnosticText.UnindentLines();

        Assert.True(
            annotatedText.Spans.Length == expectedDiagnosticMessages.Length,
            "Test invalid, must have equal number of diagnostics as text spans"
        );

        var expectedDiagnostics = expectedDiagnosticMessages
            .Zip(annotatedText.Spans)
            .Select(tuple => new { Span = tuple.Second, Message = tuple.First })
            .OrderBy(diagnostic => diagnostic.Span.Start)
            .ToArray();

        var actualDiagnostics = diagnostics
            .OrderBy(diagnostic => diagnostic.Location?.Span.Start ?? -1)
            .ToArray();

        for (var i = 0; i < expectedDiagnosticMessages.Length; i++)
        {
            Assert.True(
                i < expectedDiagnostics.Length,
                $"Expected at least {i + 1} expected diagnostics only found {expectedDiagnostics.Length}"
            );
            Assert.True(
                i < actualDiagnostics.Length,
                $"Expected at least {i + 1} actual diagnostics only found {actualDiagnostics.Length}"
            );

            var expectedMessage = expectedDiagnostics[i].Message;
            var actualMessage = actualDiagnostics[i].Message;
            Assert.Equal(expectedMessage, actualMessage);

            var expectedSpan = expectedDiagnostics[i].Span;
            var actualSpan = actualDiagnostics[i].Span;
            Assert.Equal(expectedSpan, actualSpan);
        }

        Assert.Equal(expectedDiagnosticMessages.Length, expectedDiagnostics.Length);
    }

    public static void AssertEvaluation(string code, object value, ScriptHost host)
    {
        var result = Execute(code, host);
        Assert.Equal(value, result.Value);
    }

    public static ScriptHost BuildScriptHost([CallerMemberName] string? testName = null) =>
        new ScriptHost(AssembliesWithStdLib, testName ?? "test");

    public static ScriptHost BuildScriptHostTestLib([CallerMemberName] string? testName = null) =>
        new ScriptHost(AssembliesWithTestStdLib, testName ?? "test");

    public static ExecutionResult Execute(string code, ScriptHost host)
    {
        var tree = SyntaxTree.Parse(code);
        Assert.Empty(tree.Diagnostics);

        var compilation = host.Compile(tree);

        var executionResult = host.Execute(compilation);
        Assert.Empty(executionResult.Diagnostics);

        return executionResult;
    }

    public static string BuildExpectedOutput(params string[] expectedLines)
    {
        using var sb = new StringWriter();
        foreach (var expectedLine in expectedLines)
            sb.WriteLine(expectedLine);

        return sb.ToString();
    }

    public static Compilation Compile(string code)
    {
        var tree = SyntaxTree.Parse(code);
        Assert.Empty(tree.Diagnostics);

        using var host = BuildScriptHost();

        var compilation = host.Compile(tree);

        Assert.Empty(compilation.Diagnostics);

        return compilation;
    }

    public static void AssertByteArrays(
        IList<byte> expected,
        IList<byte> actual,
        string message = ""
    )
    {
        string BinaryValue(IList<byte> bytes, int i) =>
            i < bytes.Count ? "0b" + Convert.ToString(bytes[i], 2).PadLeft(8, '0') : "MISSING";

        string BuildErrString(int i, IList<byte> expectedBytes, IList<byte> actualBytes)
        {
            var expectedBinary = BinaryValue(expectedBytes, i);
            var actualBinary = BinaryValue(actualBytes, i);

            return message
                + $"Expected: {string.Join(", ", expectedBytes.Select(x => $"0x{x:X2}"))}\r\n"
                + $"Actual:   {string.Join(", ", actualBytes.Select(x => $"0x{x:X2}"))}\r\n"
                + new string('-', 10 + i * 6)
                + "^^^^\r\n"
                + "\r\n"
                + $"At offset: {i}\r\n"
                + $"Expected: {expectedBinary}\r\n"
                + $"Actual:   {actualBinary}";
        }

        for (var i = 0; i < expected.Count; i++)
        {
            if (i >= actual.Count || expected[i] != actual[i])
            {
                throw new XunitException("\r\n" + BuildErrString(i, expected, actual));
            }
        }
    }
}
