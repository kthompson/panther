using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Xunit.Sdk;

namespace Panther.Tests.CodeAnalysis;

class Dotnet
{
    public static string[] Invoke(string assemblyLocation)
    {
        var dotnet = FindDotnet();
        using var proc = Process.Start(
            new ProcessStartInfo(dotnet)
            {
                Arguments = assemblyLocation,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            }
        );
        var errorOutput = new List<string>();
        var output = new List<string>();
        if (proc == null)
            throw new XunitException("Failed to run dotnet command");

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

    private static string? NukeLocation;

    private static string? FindDotNuke()
    {
        if (NukeLocation != null)
        {
            if (string.Empty == NukeLocation)
                return null;
            return NukeLocation;
        }
        // set to empty so we only run once
        NukeLocation = string.Empty;

        var currentDirectory = Directory.GetCurrentDirectory();
        while (true)
        {
            var nuke = Path.Combine(currentDirectory, ".nuke");
            if (Directory.Exists(nuke))
            {
                NukeLocation = nuke;
                return nuke;
            }

            var parent = Path.GetDirectoryName(currentDirectory);
            if (parent == null)
                return null;

            currentDirectory = parent;
        }
    }

    private static string FindDotnet()
    {
        // if we have a temp install from nuke, on a *nix os lets use the temp install of dotnet there
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var nuke = FindDotNuke();
            if (nuke != null)
            {
                var nukeDotnet = Path.Combine(nuke, "temp", "dotnet-unix", "dotnet");
                if (File.Exists(nukeDotnet))
                    return nukeDotnet;
            }
        }

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

        // Ok we cant find it on the path lets just take some guesses
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var homebrew = "/usr/local/share/dotnet/dotnet";
            if (File.Exists(homebrew))
                return homebrew;
        }

        throw new ArgumentException("Could not find `dotnet` executable");
    }
}
