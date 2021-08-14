using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.AzurePipelines.Configuration;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities;
using Nuke.Common.Utilities.Collections;
using Panther.Build.Components;

using static Nuke.Common.IO.FileSystemTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
[AzurePipelines(
    "pr",
    AzurePipelinesImage.UbuntuLatest,
    AzurePipelinesImage.WindowsLatest,
    AzurePipelinesImage.MacOsLatest,
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IReportCoverage.ReportCoverage) },
    NonEntryTargets = new[] { nameof(IRestore.Restore) },
    ExcludedTargets = new[] { nameof(Clean) },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj" },
    CachePath = "$(Pipeline.Workspace)/.nuget",
    PullRequestsBranchesInclude = new []{ "main" }
    )]
[AzurePipelines(
    suffix: null,
    AzurePipelinesImage.UbuntuLatest,
    AzurePipelinesImage.WindowsLatest,
    AzurePipelinesImage.MacOsLatest,
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IReportCoverage.ReportCoverage) },
    NonEntryTargets = new[] { nameof(IRestore.Restore), nameof(ICompile.Compile) },
    ExcludedTargets = new[] { nameof(Clean) },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj" },
    CachePath = "$(Pipeline.Workspace)/.nuget",
    TriggerBranchesInclude = new []{ "main" }
)]
class Build : NukeBuild,
    IHazChangelog,
    IHazGitRepository,
    IHazGitVersion,
    IHazSolution,
    IRestore,
    ICompile,
    // IPack,
    ITest,
    IReportCoverage //,
    // IReportIssues,
    // IReportDuplicates,
    // IPublish
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => ((ICompile)x).Compile);

    [CI] readonly AzurePipelines AzurePipelines = null!;

    [Solution(GenerateProjects = true)] readonly Solution Solution = null!;
    Solution IHazSolution.Solution => Solution;

    GitVersion GitVersion => From<IHazGitVersion>().Versioning;
    GitRepository GitRepository => From<IHazGitRepository>().GitRepository;


    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath OutputDirectory => RootDirectory / "artifacts";
    AbsolutePath IHazArtifacts.ArtifactsDirectory => RootDirectory / "artifacts";

    // AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    // AbsolutePath TestResultDirectory => ArtifactsDirectory / "test-results";
    // AbsolutePath CoverageReportDirectory => ArtifactsDirectory / "coverage-report";
    // AbsolutePath CoverageReportArchive => ArtifactsDirectory / "coverage-report.zip";

    Target Clean => _ => _
         .Before<IRestore>()
         .Executes(() =>
         {
             SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
             TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
             EnsureCleanDirectory(OutputDirectory);
         });

    IEnumerable<Project> ITest.TestProjects => Partition.GetCurrent(Solution.GetProjects("*.Tests"));

    bool IReportCoverage.CreateCoverageHtmlReport => true;
    bool IReportCoverage.ReportToCodecov => false;


    T From<T>()
        where T : INukeBuild
        => (T) (object) this;


    public class AzurePipelinesAttribute : Nuke.Common.CI.AzurePipelines.AzurePipelinesAttribute
    {
        private readonly AzurePipelinesImage[] _images;
        public AzurePipelinesAttribute(
            string suffix,
            AzurePipelinesImage image,
            params AzurePipelinesImage[] images)
            : base(suffix, image, images)
        {
            _images = new[] { image }.Concat(images).ToArray();
        }

        class AzurePipelinesConfiguration : Nuke.Common.CI.AzurePipelines.Configuration.AzurePipelinesConfiguration
        {
            public override void Write(CustomFileWriter writer)
            {
                using (writer.WriteBlock("variables:"))
                {
                    writer.WriteLine($"NUGET_PACKAGES: $(Pipeline.Workspace)/.nuget/packages");
                    writer.WriteLine();
                }

                base.Write(writer);
            }
        }

        public override ConfigurationEntity GetConfiguration(NukeBuild build, IReadOnlyCollection<ExecutableTarget> relevantTargets)
        {
            return new AzurePipelinesConfiguration
            {
                VariableGroups = ImportVariableGroups,
                VcsPushTrigger = GetVcsPushTrigger(),
                Stages = _images.Select(x => GetStage(x, relevantTargets)).ToArray()
            };
        }
    }
}