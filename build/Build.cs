using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.AzurePipelines;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.Coverlet;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.ReportGenerator;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.CompressionTasks;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;
using static Nuke.Common.Tools.ReportGenerator.ReportGeneratorTasks;

[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
[AzurePipelines(
    "pr",
    AzurePipelinesImage.UbuntuLatest,
    AzurePipelinesImage.WindowsLatest,
    AzurePipelinesImage.MacOsLatest,
    InvokedTargets = new[] { nameof(Test) },
    NonEntryTargets = new[] { nameof(Restore) },
    ExcludedTargets = new[] { nameof(Clean) },
    PullRequestsBranchesInclude = new []{ "main" }
    )]
[AzurePipelines(
    suffix: null,
    AzurePipelinesImage.UbuntuLatest,
    AzurePipelinesImage.WindowsLatest,
    AzurePipelinesImage.MacOsLatest,
    InvokedTargets = new[] { nameof(Test) },
    NonEntryTargets = new[] { nameof(Restore) },
    ExcludedTargets = new[] { nameof(Clean) },
    TriggerBranchesInclude = new []{ "main" }
)]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [CI] readonly AzurePipelines AzurePipelines;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")] readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;
    //[GitVersion] private readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath TestsDirectory => RootDirectory / "tests";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath TestResultDirectory => ArtifactsDirectory / "test-results";
    AbsolutePath CoverageReportDirectory => ArtifactsDirectory / "coverage-report";
    AbsolutePath CoverageReportArchive => ArtifactsDirectory / "coverage-report.zip";

    Target Clean => _ => _
         .Before(Restore)
         .Executes(() =>
         {
             SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
             TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
             EnsureCleanDirectory(ArtifactsDirectory);
         });

    Target Restore => _ => _
         .Executes(() =>
         {
             DotNetRestore(s => s
                 .SetProjectFile(Solution));
         });

    Target Compile => _ => _
         .DependsOn(Restore)
         .Executes(() =>
         {
             DotNetBuild(s => s
                 .SetProjectFile(Solution)
                 .SetConfiguration(Configuration)
                 //.SetAssemblyVersion(GitVersion.AssemblySemVer)
                 //.SetFileVersion(GitVersion.AssemblySemFileVer)
                 //.SetInformationalVersion(GitVersion.InformationalVersion)
                 .EnableNoRestore());
         });

    [Partition(1)] readonly Partition TestPartition;
    IEnumerable<Project> TestProjects => TestPartition.GetCurrent(Solution.GetProjects("*.Tests"));

    Target Test => _ => _
        .DependsOn(Compile)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetResultsDirectory(TestResultDirectory)
                .When(InvokedTargets.Contains(Coverage) || IsServerBuild, _ => _
                    .EnableCollectCoverage()
                    .SetCoverletOutputFormat(CoverletOutputFormat.cobertura)
                    .SetExcludeByFile("*.Generated.cs")
                    .When(IsServerBuild, _ => _.EnableUseSourceLink()))
                .CombineWith(TestProjects, (_, v) => _
                    .SetProjectFile(v)
                    .SetLogger($"trx;LogFileName={v.Name}.trx")
                    .When(InvokedTargets.Contains(Coverage) || IsServerBuild, _ => _
                        .SetCoverletOutput(TestResultDirectory / $"{v.Name}.xml"))));

            TestResultDirectory
                .GlobFiles("*.trx")
                .ForEach(x =>
                    AzurePipelines?.PublishTestResults(
                        type: AzurePipelinesTestResultsType.VSTest,
                        title: $"{Path.GetFileNameWithoutExtension(x)} ({AzurePipelines.StageDisplayName})",
                        files: new string[] { x }));
        });


    Target Coverage => _ => _
          .DependsOn(Test)
          .TriggeredBy(Test)
          .Consumes(Test)
          .Produces(CoverageReportArchive)
          .Executes(() =>
          {
              ReportGenerator(_ => _
                  .SetReports(TestResultDirectory / "*.xml")
                  .SetReportTypes(ReportTypes.HtmlInline)
                  .SetTargetDirectory(CoverageReportDirectory)
                  .SetFramework("netcoreapp2.1"));

              TestResultDirectory.GlobFiles("*.xml").ForEach(x =>
                  AzurePipelines?.PublishCodeCoverage(
                      AzurePipelinesCodeCoverageToolType.Cobertura,
                      x,
                      CoverageReportDirectory));

              CompressZip(
                  directory: CoverageReportDirectory,
                  archiveFile: CoverageReportArchive,
                  fileMode: FileMode.Create);
          });
}