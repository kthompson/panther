using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.Utilities.Collections;
using Nuke.Components;

using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.Tools.ReSharper.ReSharperTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
[GitHubActions("default", GitHubActionsImage.WindowsLatest, GitHubActionsImage.UbuntuLatest,
    GitHubActionsImage.MacOsLatest,
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IReportCoverage.ReportCoverage), nameof(IPublish.Publish) },
    OnPushBranches = new[] { "main" },
    EnableGitHubContext = true,
    ImportSecrets = new[]
    {
        nameof(IReportCoverage.CodecovToken),
        nameof(PublicNuGetApiKey),
    },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj" }
)]
[GitHubActions("pr", GitHubActionsImage.WindowsLatest, GitHubActionsImage.UbuntuLatest, GitHubActionsImage.MacOsLatest,
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IReportCoverage.ReportCoverage), nameof(IPublish.Publish) },
    OnPullRequestBranches = new[] { "main" },
    EnableGitHubContext = true,
    ImportSecrets = new[]
    {
        nameof(IReportCoverage.CodecovToken)
    },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj" }
)]
class Build : NukeBuild,
    IHazChangelog,
    IHazGitRepository,
    IHazNerdbankGitVersioning,
    IHazSolution,
    IRestore,
    ICompile,
    IPack,
    ITest,
    IReportCoverage,
    IReportIssues,
    IReportDuplicates,
    IPublish
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => ((IPack)x).Pack);

    [CI] readonly GitHubActions GitHubActions;

    NerdbankGitVersioning GitVersion => From<IHazNerdbankGitVersioning>().Versioning;
    GitRepository GitRepository => From<IHazGitRepository>().GitRepository;

    [Solution(GenerateProjects = true)] readonly Solution Solution = null!;
    Nuke.Common.ProjectModel.Solution IHazSolution.Solution => Solution;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";

    Target Clean => _ => _
        .Before<IRestore>()
        .Executes(() =>
        {
            Serilog.Log.Information("GitHub Context: {Context}", GitHubActions.GitHubContext.ToString(Formatting.Indented));

            SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
            EnsureCleanDirectory(From<IHazArtifacts>().ArtifactsDirectory);
        });

    IEnumerable<Project> ITest.TestProjects => Partition.GetCurrent(Solution.GetProjects("*.Tests"));

    bool IReportCoverage.CreateCoverageHtmlReport => true;

    // only the windows image on GitHub has the 3.0.0 dotnet runtime that the Codecov tool needs
    bool IReportCoverage.ReportToCodecov => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    IEnumerable<(string PackageId, string Version)> IReportIssues.InspectCodePlugins
        => new (string PackageId, string Version)[]
        {
            new("ReSharperPlugin.CognitiveComplexity", ReSharperPluginLatest)
        };

    bool IReportIssues.InspectCodeFailOnWarning => false;
    bool IReportIssues.InspectCodeReportWarnings => true;
    IEnumerable<string> IReportIssues.InspectCodeFailOnIssues => new[] { "CognitiveComplexity" };
    IEnumerable<string> IReportIssues.InspectCodeFailOnCategories => Array.Empty<string>();


    string PublicNuGetSource => "https://api.nuget.org/v3/index.json";

    string GitHubRegistrySource => GitHubActions != null
        ? $"https://nuget.pkg.github.com/{GitHubActions.RepositoryOwner}/index.json"
        : null;

    [Parameter] [Secret] readonly string PublicNuGetApiKey;

    // only publish from one of the three platforms
    IEnumerable<AbsolutePath> IPublish.PushPackageFiles =>
        RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
            ? From<IPack>().PackagesDirectory.GlobFiles("*.nupkg")
            : Array.Empty<AbsolutePath>();

    string IPublish.NuGetApiKey => GitRepository.IsOnMainBranch() ? PublicNuGetApiKey : GitHubActions.Token;
    string IPublish.NuGetSource => GitRepository.IsOnMainBranch() ? PublicNuGetSource : GitHubRegistrySource;

    T From<T>()
        where T : INukeBuild
        => (T)(object)this;
}