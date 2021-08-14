using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.ValueInjection;

namespace Panther.Build.Components
{
    public interface IHazGitRepository : INukeBuild
    {
        [GitRepository] [Required] GitRepository GitRepository => ValueInjectionUtility.TryGetValue(() => GitRepository);
    }
}