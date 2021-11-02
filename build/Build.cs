using System;
using System.Collections.Generic;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.CI.AzurePipelines.Configuration;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
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
    CachePath = "$(NUGET_PACKAGES)",
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
    CachePath = "$(NUGET_PACKAGES)",
    TriggerBranchesInclude = new []{ "main" }
)]
[GitHubActions("default", GitHubActionsImage.WindowsLatest, GitHubActionsImage.UbuntuLatest, GitHubActionsImage.MacOsLatest,
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IReportCoverage.ReportCoverage) },
    // NonEntryTargets = new[] { nameof(IRestore.Restore), nameof(ICompile.Compile) },
    // ExcludedTargets = new[] { nameof(Clean) },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj" },
    // CachePath = "$(NUGET_PACKAGES)",
    OnPushBranches = new []{ "main" }
)]
[GitHubActions("pr", GitHubActionsImage.WindowsLatest, GitHubActionsImage.UbuntuLatest, GitHubActionsImage.MacOsLatest,
    InvokedTargets = new[] { nameof(ITest.Test), nameof(IReportCoverage.ReportCoverage) },

    // NonEntryTargets = new[] { nameof(IRestore.Restore) },
    // ExcludedTargets = new[] { nameof(Clean) },
    CacheKeyFiles = new[] { "global.json", "src/**/*.csproj" },
    // CachePath = "$(NUGET_PACKAGES)",
    OnPullRequestBranches = new [] { "main" }
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

    static Dictionary<string, string> CustomNames =
        new Dictionary<string, string>
        {
            { nameof(ICompile.Compile), "⚙️" },
            { nameof(ITest.Test), "🚦" },
            // { nameof(IPack.Pack), "📦" },
            { nameof(IReportCoverage.ReportCoverage), "📊" },
            // { nameof(IReportDuplicates.ReportDuplicates), "🎭" },
            // { nameof(IReportIssues.ReportIssues), "💣" },
            // { nameof(ISignPackages.SignPackages), "🔑" },
            // { nameof(IPublish.Publish), "🚚" },
            // { nameof(Announce), "🗣" }
        };

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

        class AzurePipelinesDownloadStep : AzurePipelinesStep
        {
            public string ArtifactName { get; set; }
            public string DownloadPath { get; set; }

            public override void Write(CustomFileWriter writer)
            {
                using (writer.WriteBlock("- task: DownloadBuildArtifacts@0"))
                {
                    writer.WriteLine("displayName: Download Artifacts");
                    using (writer.WriteBlock("inputs:"))
                    {
                        // writer.WriteLine("buildType: 'current'");
                        // writer.WriteLine("downloadType: 'single'");
                        writer.WriteLine($"artifactName: {ArtifactName}");
                        writer.WriteLine($"downloadPath: {DownloadPath.SingleQuote()}");
                    }
                }
            }
        }

        protected override IEnumerable<AzurePipelinesStep> GetSteps(
            ExecutableTarget executableTarget,
            IReadOnlyCollection<ExecutableTarget> relevantTargets)
        {
            var steps = base.GetSteps(executableTarget, relevantTargets).ToList();

            // executableTarget.ArtifactDependencies

            // var artifactDependencies = (
            //     from artifactDependency in executableTarget.ArtifactDependencies[executableTarget]
            //     let dependency = executableTarget.ExecutionDependencies.Single(x => x.Factory == artifactDependency.Item1)
            //
            //     // select new TeamCityArtifactDependency
            //     //        {
            //     //            BuildType = buildTypes[dependency].Single(x => x.Partition == null),
            //     //            ArtifactRules = rules
            //     //        }
            //     select 1
            //     ).ToArray();


            // foreach (var executableTargetArtifactDependency in executableTarget.ArtifactDependencies[executableTarget])
            // {
            //
            //     Console.WriteLine(executableTargetArtifactDependency);
            //     // foreach (var s in executableTargetArtifactDependency)
            //     // {
            //     //     Console.WriteLine(s);
            //     // }
            //
            // }

            // throw new NotImplementedException();


            static string GetArtifactPath(AbsolutePath path)
                => NukeBuild.RootDirectory.Contains(path)
                    ? NukeBuild.RootDirectory.GetUnixRelativePathTo(path)
                    : (string) path;


            var dependentArtifacts = executableTarget.AllDependencies
                .SelectMany(dep => dep.ArtifactProducts)
                .Select(x => (AbsolutePath)x)
                .Select(x => x.DescendantsAndSelf(y => y.Parent).FirstOrDefault(y => !y.ToString().ContainsOrdinalIgnoreCase("*")))
                .Distinct()
                .Select(GetArtifactPath)
                .Select(publishedArtifact =>
                {
                    var artifactName = publishedArtifact.Split('/').Last();
                    return new AzurePipelinesDownloadStep
                    {
                        ArtifactName = artifactName,
                        DownloadPath = publishedArtifact.TrimEnd($"/{artifactName}")
                    };
                })
                .ToArray();

            // https://github.com/kthompson/panther/commit/22c54f3221aa5aee12f7d1c7ba955d26f6db4137

            var index = steps.FindIndex(step => step is AzurePipelinesCmdStep);

            steps.InsertRange(index, dependentArtifacts);

            return steps;
        }

        protected override AzurePipelinesJob GetJob(ExecutableTarget executableTarget, LookupTable<ExecutableTarget, AzurePipelinesJob> jobs, IReadOnlyCollection<ExecutableTarget> relevantTargets)
        {
            var job = base.GetJob(executableTarget, jobs, relevantTargets);

            var symbol = CustomNames.GetValueOrDefault(job.Name).NotNull("symbol != null");
            job.DisplayName = job.Parallel == 0
                ? $"{symbol} {job.DisplayName}"
                : $"{symbol} {job.DisplayName} 🧩";
            return job;
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