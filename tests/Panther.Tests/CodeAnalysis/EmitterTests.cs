using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using Panther.CodeAnalysis;
using Panther.CodeAnalysis.Syntax;
using Xunit;
using Xunit.Sdk;

namespace Panther.Tests.CodeAnalysis
{
    public class EmitterTests
    {

        public static readonly string SourceRoot = Path.Combine("..", "..", "..", "..", "..");
        public static readonly string OutputPath = Path.Combine("CodeAnalysis", "EmitterTests");
        public static readonly string ToolsPath = Path.Combine(SourceRoot, "tools");
        public static readonly string IlSpyPath = Path.Combine(ToolsPath, "ilspycmd.exe");
        public static readonly string EmitterTestsPath = Path.Combine(SourceRoot, "tests");

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
                new[] {typeof(object).Assembly.Location, typeof(Console).Assembly.Location},
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
            var actualOutputTxtPath = Path.Combine(outputDirectory, "output.txt");
            var scriptPath = CreateOutputScript(outputDirectory, assemblyLocation);

            using var proc = Process.Start(new ProcessStartInfo(scriptPath)
            {
                UseShellExecute = true,

                CreateNoWindow = true,
                WorkingDirectory = outputDirectory
            });
            proc.WaitForExit();

            var actualOutputTxt = File.ReadAllLines(actualOutputTxtPath);

            var expectedOutputTxt = File.ReadAllLines(outputTxtPath);
            AssertFileLines(expectedOutputTxt, actualOutputTxt);
        }

        private static string CreateOutputScript(string outputDirectory, string assemblyLocation)
        {
            string scriptPath;
            if (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
            {
                scriptPath = Path.Combine(outputDirectory, "createOutput.sh");
                File.WriteAllText(scriptPath, $@"dotnet {assemblyLocation} > output.txt 2>&1");
            }
            else
            {
                scriptPath = Path.Combine(outputDirectory, "createOutput.cmd");
                File.WriteAllText(scriptPath, $@"dotnet {assemblyLocation} > output.txt 2>&1");
            }

            return scriptPath;
        }

        private static void AssertFileLines(string[] expectedLines, string[] actualLines)
        {
            for (var i = 0; i < actualLines.Length; i++)
            {
                Assert.True(expectedLines.Length >= i, $"Missing line {i + 1}. Expected: {actualLines[i]}");
                if (expectedLines[i].Trim() != actualLines[i].Trim())
                    throw new AssertActualExpectedException(expectedLines[i].Trim(), actualLines[i].Trim(),
                        $"Line {i + 1}");
            }
        }

        private static string DumpIl(string assemblyLocation)
        {
            using var sw = new StringWriter();
            using var module = new PEFile(assemblyLocation);

            sw.WriteLine($"// IL code: {module.Name}");
            var disassembler = new ReflectionDisassembler(new PlainTextOutput(sw), CancellationToken.None);
            disassembler.WriteModuleContents(module);

            return sw.ToString();
        }

        public static IEnumerable<object[]> GetEmitterTests()
        {
            foreach (var directory in Directory.GetDirectories(OutputPath))
            {
                var sources = Directory.GetFiles(directory, "*.pn");
                var expected = Directory.GetFiles(directory, "*.il").SingleOrDefault();
                yield return new object[] {directory, sources, expected };
            }
        }
    }
}