using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Xunit;
using Xunit.Sdk;

namespace Panther.Tests.CodeAnalysis
{
    class Dotnet
    {
        public static string Invoke(string assemblyLocation)
        {
            var dotnet = FindDotnet();
            using var proc = Process.Start(new ProcessStartInfo(dotnet)
            {
                Arguments = assemblyLocation,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            });
            var errorOutput = new StringBuilder();
            var output = new StringBuilder();
            proc.ErrorDataReceived += (sender, args) => errorOutput.Append(args.Data);
            proc.OutputDataReceived += (sender, args) => output.Append(args.Data);
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            proc.WaitForExit();

            var errorText = errorOutput.ToString();
            if (!string.IsNullOrEmpty(errorText))
                throw new XunitException("Failed to run dotnet command: " + errorText);

            return output.ToString();
        }

        private static string FindDotnet()
        {
            var fileName = "dotnet";
            var values = Environment.GetEnvironmentVariable("PATH");
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