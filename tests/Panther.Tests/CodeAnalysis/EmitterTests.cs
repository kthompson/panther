﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
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
            var outputPath = Path.Combine(Path.GetTempPath(), "Panther", "EmitterTests", moduleName);

            Directory.CreateDirectory(outputPath);
            try
            {
                var assemblyLocation = Path.Combine(outputPath, moduleName + ".dll");
                var results = compilation.Emit(moduleName, new[] { typeof(object).Assembly.Location, typeof(Console).Assembly.Location },
                    assemblyLocation);

                Assert.Empty(results);

                using var proc = Process.Start(IlSpyPath, $"{assemblyLocation} -il -o {outputPath}");
                Assert.NotNull(proc);
                proc.WaitForExit();

                var ilLocation = Path.Combine(outputPath, moduleName + ".il");
                var actualSource = File.ReadAllLines(ilLocation);
                for (int i = 0; i < actualSource.Length; i++)
                {
                    Assert.True(expectedSource.Length >= i, $"Missing line {i + 1}. Expected: {actualSource[i]}");
                    if (expectedSource[i].Trim() != actualSource[i].Trim())
                        throw new AssertActualExpectedException(expectedSource[i].Trim(), actualSource[i].Trim(),
                            $"Line {i + 1}");
                }

                Assert.Equal(expectedSource.Length, actualSource.Length);
            }
            finally
            {
                // Directory.Delete(outputPath, true);
            }
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