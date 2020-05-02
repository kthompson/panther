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
using Panther.CodeAnalysis.Binding;
using Panther.CodeAnalysis.Symbols;
using Panther.CodeAnalysis.Syntax;
using Panther.CodeAnalysis.Text;
using Xunit;


namespace Panther.Tests.CodeAnalysis
{
    public static class TestHelpers
    {
        public static ImmutableArray<AssemblyDefinition> AssembliesWithStdLib { get; }
        public static ImmutableArray<AssemblyDefinition> AssembliesWithTestStdLib { get; }
        public static string b(bool value) => value ? "true" : "false";

        static TestHelpers()
        {
            AssembliesWithTestStdLib = GetAssemblyDefinitions(typeof(object).Assembly.Location,
                typeof(Console).Assembly.Location, typeof(TestLib::Panther.Unit).Assembly.Location);

            AssembliesWithStdLib = GetAssemblyDefinitions(typeof(object).Assembly.Location,
                typeof(Console).Assembly.Location, typeof(StdLib::Panther.Unit).Assembly.Location);
        }

        private static ImmutableArray<AssemblyDefinition> GetAssemblyDefinitions(params string[] references)
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


        public static void AssertHasDiagnostics(string text, string diagnosticText, [CallerMemberName] string? testName = null)
        {
            var annotatedText = AnnotatedText.Parse(text);
            var syntaxTree = SyntaxTree.Parse(annotatedText.Text);
            using var scriptHost = new ScriptHost(AssembliesWithStdLib, testName ?? "test");

            var result = scriptHost.Execute(syntaxTree);
            var expectedDiagnosticMessages = AnnotatedText.UnindentLines(diagnosticText);

            Assert.True(annotatedText.Spans.Length == expectedDiagnosticMessages.Length, "Test invalid, must have equal number of diagnostics as text spans");

            var expectedDiagnostics = expectedDiagnosticMessages.Zip(annotatedText.Spans)
                .Select(tuple => new { Span = tuple.Second, Message = tuple.First })
                .OrderBy(diagnostic => diagnostic.Span.Start)
                .ToArray();

            var actualDiagnostics = result.Diagnostics.OrderBy(diagnostic => diagnostic.Location?.Span.Start ?? -1).ToArray();

            for (var i = 0; i < expectedDiagnosticMessages.Length; i++)
            {
                Assert.True(expectedDiagnostics.Length > i, $"Expected more diagnostics ({expectedDiagnosticMessages.Length}) than actual {expectedDiagnostics.Length}");

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
    }
}