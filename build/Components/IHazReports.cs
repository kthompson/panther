using Nuke.Common.IO;

namespace Panther.Build.Components
{
    public interface IHazReports : IHazArtifacts
    {
        AbsolutePath ReportDirectory => ArtifactsDirectory / "reports";
    }
}