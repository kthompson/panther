using Nuke.Common;
using Nuke.Common.IO;

namespace Panther.Build.Components
{
    public interface IHazArtifacts : INukeBuild
    {
        AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    }
}