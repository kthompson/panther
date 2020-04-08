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
    suffix: null,
    AzurePipelinesImage.UbuntuLatest,
    AzurePipelinesImage.WindowsLatest,
    AzurePipelinesImage.MacOsLatest,
    InvokedTargets = new[] { nameof(Test) },
    NonEntryTargets = new[] { nameof(Restore) },
    ExcludedTargets = new[] { nameof(Clean), nameof(Coverage) }
    )]
internal class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [CI] private readonly AzurePipelines AzurePipelines;

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    private readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] private readonly Solution Solution;
    [GitRepository] private readonly GitRepository GitRepository;
    //[GitVersion] private readonly GitVersion GitVersion;

    private AbsolutePath SourceDirectory => RootDirectory / "src";
    private AbsolutePath TestsDirectory => RootDirectory / "tests";
    private AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    private Target Clean => _ => _
         .Before(Restore)
         .Executes(() =>
         {
             SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
             TestsDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
             EnsureCleanDirectory(ArtifactsDirectory);
         });

    private Target Restore => _ => _
         .Executes(() =>
         {
             DotNetRestore(s => s
                 .SetProjectFile(Solution));
         });

    private Target Compile => _ => _
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

    [Partition(1)] private readonly Partition TestPartition;
    AbsolutePath TestResultDirectory => ArtifactsDirectory / "test-results";
    IEnumerable<Project> TestProjects => TestPartition.GetCurrent(Solution.GetProjects("*.Tests"));

    private Target Test => _ => _
        .DependsOn(Compile)
        .Produces(TestResultDirectory / "*.trx")
        .Produces(TestResultDirectory / "*.xml")
        .Partition(() => TestPartition)
        .Executes(() =>
        {
            DotNetTest(s => s
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetResultsDirectory(TestResultDirectory)
                .SetEnvironmentVariable("BUILD_SERVER", "true")
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

            ArtifactsDirectory.GlobFiles("*.trx").ForEach(x =>
                AzurePipelines?.PublishTestResults(
                    type: AzurePipelinesTestResultsType.XUnit,
                    title: $"{Path.GetFileNameWithoutExtension(x)} ({AzurePipelines.StageDisplayName})",
                    files: new string[] { x }));
        });

    private string CoverageReportDirectory => ArtifactsDirectory / "coverage-report";
    private string CoverageReportArchive => ArtifactsDirectory / "coverage-report.zip";

    private Target Coverage => _ => _
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