using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.ValueInjection;

namespace Panther.Build.Components
{
    interface IRestore : IHazSolution, INukeBuild
    {
        Target Restore => _ => _
            .Executes(() =>
            {
                DotNetTasks.DotNetRestore(_ => _
                    .Apply(RestoreSettingsBase)
                    .Apply(RestoreSettings));
            });

        sealed Configure<DotNetRestoreSettings> RestoreSettingsBase => _ => _
            .SetProjectFile(Solution)
            .SetIgnoreFailedSources(IgnoreFailedSources);
        // RestorePackagesWithLockFile
        // .SetProperty("RestoreLockedMode", true));

        Configure<DotNetRestoreSettings> RestoreSettings => _ => _;

        [Parameter("Ignore unreachable sources during " + nameof(Restore))]
        bool IgnoreFailedSources => ValueInjectionUtility.TryGetValue<bool?>(() => IgnoreFailedSources) ?? false;
    }
}