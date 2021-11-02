extern alias StdLib;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.CSharp;
using Panther.CodeAnalysis.Syntax;
using Xunit;
using Xunit.Sdk;
using Unit = StdLib::Panther.Unit;

namespace Panther.Tests.CodeAnalysis
{
    public class EmitterTests
    {
        public static readonly string OutputPath = Path.Combine("CodeAnalysis", "Emit");

        (string moduleName, string outputDirectory) GetModuleNameAndOutputDirectory(string testDirectory)
        {
            var moduleName = Path.GetFileName(testDirectory);
            Assert.NotNull(moduleName);
            var outputDirectory = Path.Combine(Path.GetTempPath(), "Panther", "Emit", moduleName);
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
            Directory.CreateDirectory(outputDirectory);

            return (moduleName, outputDirectory);
        }


        [Theory]
        [MemberData(nameof(GetCSharpEmitterTests))]
        public void CSharpEmitterOutputsCSharp(string pantherSource, string csharpSource)
        {
            var tree = SyntaxTree.LoadFile(pantherSource);
            Assert.Empty(tree.Diagnostics);

            var output = CSharpEmitter.ToCSharpText(tree);

            var expectedSource = File.ReadAllText(csharpSource)
                .Split(Environment.NewLine)
                .ToArray();

            var actualSource = output.Split(Environment.NewLine).ToArray();
            AssertFileLines(expectedSource, actualSource);

            Assert.Equal(expectedSource.Length, actualSource.Length);
        }

        [Theory]
        [MemberData(nameof(GetEmitterTests))]
        public void EmitterOutputsIL(string testDirectory, string[] sources, string expectedILPath)
        {
            var expectedSource = File.ReadAllLines(expectedILPath).Where(line => !line.TrimStart().StartsWith("//")).ToArray();

            var trees = sources.Select(SyntaxTree.LoadFile).ToArray();

            foreach (var tree in trees)
                Assert.Empty(tree.Diagnostics);

            var (moduleName, outputDirectory) = GetModuleNameAndOutputDirectory(testDirectory);

            Directory.CreateDirectory(outputDirectory);

            var references = new[]
            {
                typeof(object).Assembly.Location,
                typeof(Console).Assembly.Location,
                typeof(Unit).Assembly.Location
            };

            foreach (var reference in references)
            {
                var referenceCopy = Path.Combine(outputDirectory, Path.GetFileName(reference));
                TryCopy(reference, referenceCopy);
            }

            var (diagnostics, compilation) = Compilation.Create(references, trees);
            Assert.Empty(diagnostics);
            Assert.NotNull(compilation); Debug.Assert(compilation != null);

            var assemblyLocation = Path.Combine(outputDirectory, moduleName + ".dll");

            var emitResult = compilation.Emit(
                moduleName,
                assemblyLocation);

            Assert.Empty(emitResult.Diagnostics);
            // var vmOutput = Path.Combine(outputDirectory, moduleName);
            // Directory.CreateDirectory(vmOutput);
            // var emitVm = compilation.EmitVM(vmOutput);

            var il = DumpIl(assemblyLocation);
            var actualSource = il.Split('\n').Where(line => !line.TrimStart().StartsWith("//")).ToArray();
            AssertFileLines(expectedSource, actualSource);

            Assert.Equal(expectedSource.Length, actualSource.Length);

            // verify command output
            AssertCommandOutput(testDirectory, outputDirectory, assemblyLocation);

            Directory.Delete(outputDirectory, true);
        }

        private static void TryCopy(string src, string dst)
        {
            try
            {
                File.Copy(src, dst);
            }
            catch
            {
                // ignored
            }
        }

        private static void AssertCommandOutput(string testDirectory, string outputDirectory, string assemblyLocation)
        {
            var outputTxtPath = Path.Combine(testDirectory, "output.txt");
            if (!File.Exists(outputTxtPath)) return;

            // create runtimeconfig.json

            var runtimeConfig = assemblyLocation.Substring(0, assemblyLocation.Length - 3) + "runtimeconfig.json";
            File.WriteAllText(runtimeConfig, $@"{{
  ""runtimeOptions"": {{
    ""tfm"": ""net5.0"",
    ""framework"": {{
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": ""{Environment.Version}""
    }}
  }}
}}");

            // Run emitted assembly
            var actualOutputTxt = Dotnet.Invoke(assemblyLocation);
            var expectedOutputTxt = File.ReadAllLines(outputTxtPath);
            AssertFileLines(expectedOutputTxt, actualOutputTxt);
        }

        private static void AssertFileLines(string[] expectedLines, string[] actualLines)
        {
            for (var i = 0; i < expectedLines.Length; i++)
            {
                Assert.True(actualLines.Length > i, $"Missing line {i + 1}. Expected: {expectedLines[i]}");
                if (expectedLines[i].Trim() != actualLines[i].Trim())
                    throw new AssertActualExpectedException(expectedLines[i].Trim(), actualLines[i].Trim(),
                        $"Line {i + 1}");
            }

            Assert.Equal(expectedLines.Length, actualLines.Length);
        }

        private static string DumpIl(string assemblyLocation)
        {
            using var file = new StreamWriter(assemblyLocation.Substring(0, assemblyLocation.Length - 3) + "txt");
            DumpIl(file, assemblyLocation);

            using var sw = new StringWriter();
            DumpIl(sw, assemblyLocation);

            return sw.ToString();
        }

        private static void DumpIl(TextWriter sw, string assemblyLocation)
        {
            using var module = new PEFile(assemblyLocation);

            sw.WriteLine($"// IL code: {module.Name}");
            var disassembler = new ReflectionDisassembler(new PlainTextOutput(sw), CancellationToken.None);
            disassembler.WriteModuleContents(module);
        }

        public static IEnumerable<object?[]> GetEmitterTests() =>
            from directory in Directory.GetDirectories(OutputPath)
            // HACK: currently the compilation order of files will dictate whether a function is out of scope
            // for now lets make sure we have a predictable sort order so that we can ensure the correct
            // compilation order for our sources
            let sources = Directory.GetFiles(directory, "*.pn").OrderBy(x => x).ToArray()
            let expected = Directory.GetFiles(directory, "*.il").SingleOrDefault()
            where expected != null
            select new object?[] { directory, sources, expected };

        public static IEnumerable<object?[]> GetCSharpEmitterTests() =>
            from directory in Directory.GetDirectories(OutputPath)
            from pantherSource in Directory.GetFiles(directory, "*.pn")
            let name = Path.GetFileNameWithoutExtension(pantherSource)
            let csharpSource = Path.Combine(directory, name + ".cs")
            where File.Exists(csharpSource)
            select new object?[] { pantherSource, csharpSource };
    }
}