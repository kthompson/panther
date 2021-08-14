using Nuke.Common;
using Nuke.Common.Tools.NerdbankGitVersioning;
using Nuke.Common.ValueInjection;

namespace Panther.Build.Components
{
    public interface IHazNerdbankGitVersioning
    {
        [NerdbankGitVersioning] [Required] NerdbankGitVersioning Versioning => ValueInjectionUtility.TryGetValue(() => Versioning);
    }
}