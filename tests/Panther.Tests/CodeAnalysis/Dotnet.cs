using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Panther.Tests.CodeAnalysis
{
    class Dotnet
    {
        public static string[] Invoke(string assemblyLocation)
        {
            var dotnet = FindDotnet();
            using var proc = Process.Start(new ProcessStartInfo(dotnet)
            {
                Arguments = assemblyLocation,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });
            var errorOutput = new List<string>();
            var output = new List<string>();
            proc.ErrorDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    errorOutput.Add(args.Data);
            };
            proc.OutputDataReceived += (sender, args) =>
            {
                if (args.Data != null)
                    output.Add(args.Data);
            };
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit();

            var errorText = string.Join(Environment.NewLine, errorOutput);
            if (!string.IsNullOrEmpty(errorText))
                throw new XunitException("Failed to run dotnet command: " + errorText);

            return output.ToArray();
        }

        private static string FindDotnet()
        {
            var fileName = "dotnet";
            var values = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var path in values.Split(Path.PathSeparator))
            {
                var fullPath = Path.Combine(path, fileName);
                if (File.Exists(fullPath))
                    return fullPath;

                var exeFullPath = fullPath + ".exe";
                if (File.Exists(exeFullPath))
                    return exeFullPath;
            }

            throw new ArgumentException("Could not find `dotnet` executable");
        }
    }
}