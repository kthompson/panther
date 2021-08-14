using Nuke.Common;
using static Nuke.Common.ChangeLog.ChangelogTasks;

namespace Panther.Build.Components
{
    public interface IHazChangelog : INukeBuild
    {
        // TODO: assert file exists
        string ChangelogFile => RootDirectory / "CHANGELOG.md";
        string NuGetReleaseNotes => GetNuGetReleaseNotes(ChangelogFile, (this as IHazGitRepository)?.GitRepository);
    }
}