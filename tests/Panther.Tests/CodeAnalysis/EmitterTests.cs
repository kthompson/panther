using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Syntax;
using Panther.StdLib;
using Xunit;
using Xunit.Sdk;

namespace Panther.Tests.CodeAnalysis
{
    public class EmitterTests
    {
        public static readonly string OutputPath = Path.Combine("CodeAnalysis", "EmitterTests");

        [Theory]
        [MemberData(nameof(GetEmitterTests))]
        public void EmitterOutputsIL(string testDirectory, string[] sources, string expectedILPath)
        {
            var expectedSource = File.ReadAllLines(expectedILPath);

            var trees = sources.Select(SyntaxTree.LoadFile).ToArray();

            foreach (var tree in trees)
                Assert.Empty(tree.Diagnostics);

            var moduleName = Path.GetFileName(testDirectory);
            Assert.NotNull(moduleName);
            var compilation = Compilation.Create(trees);
            var outputDirectory = Path.Combine(Path.GetTempPath(), "Panther", "EmitterTests", moduleName);

            Directory.CreateDirectory(outputDirectory);
            var assemblyLocation = Path.Combine(outputDirectory, moduleName + ".dll");
            var results = compilation.Emit(
                moduleName,
                new[]
                {
                    typeof(object).Assembly.Location,
                    typeof(Console).Assembly.Location,
                    typeof(Unit).Assembly.Location
                },
                assemblyLocation);

            Assert.Empty(results);

            var actualSource = DumpIl(assemblyLocation).Split('\n');
            AssertFileLines(expectedSource, actualSource);

            Assert.Equal(expectedSource.Length, actualSource.Length);

            // verify command output
            AssertCommandOutput(testDirectory, outputDirectory, assemblyLocation);

            Directory.Delete(outputDirectory, true);
        }

        private static void AssertCommandOutput(string testDirectory, string outputDirectory, string assemblyLocation)
        {
            var outputTxtPath = Path.Combine(testDirectory, "output.txt");
            if (!File.Exists(outputTxtPath)) return;

            // create runtimeconfig.json

            var runtimeConfig = assemblyLocation.Substring(0, assemblyLocation.Length - 3) + "runtimeconfig.json";
            File.WriteAllText(runtimeConfig, @"{
  ""runtimeOptions"": {
    ""tfm"": ""netcoreapp3.0"",
    ""framework"": {
      ""name"": ""Microsoft.NETCore.App"",
      ""version"": ""3.0.0""
    }
  }
}");

            // Build script
            var output = Dotnet.Invoke(assemblyLocation);

            var actualOutputTxt = output.Split('\n');
            var expectedOutputTxt = File.ReadAllLines(outputTxtPath);
            AssertFileLines(expectedOutputTxt, actualOutputTxt);
        }

        private static void AssertFileLines(string[] expectedLines, string[] actualLines)
        {
            for (var i = 0; i < expectedLines.Length; i++)
            {
                Assert.True(actualLines.Length > i, $"Missing line {i + 1}. Expected: {actualLines[i]}");
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

        public static IEnumerable<object[]> GetEmitterTests()
        {
            foreach (var directory in Directory.GetDirectories(OutputPath))
            {
                var sources = Directory.GetFiles(directory, "*.pn");
                var expected = Directory.GetFiles(directory, "*.il").SingleOrDefault();
                yield return new object[] { directory, sources, expected };
            }
        }
    }
}