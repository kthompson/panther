using Nuke.Common;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.ValueInjection;

namespace Panther.Build.Components
{
    public interface IHazGitVersion : INukeBuild
    {
        [GitVersion(Framework = "net5.0", NoFetch = true)]
        [Required]
        GitVersion Versioning => ValueInjectionUtility.TryGetValue(() => Versioning);
    }
}