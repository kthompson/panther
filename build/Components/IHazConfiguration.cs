using System.ComponentModel;
using Nuke.Common;
using Nuke.Common.Tooling;
using Nuke.Common.ValueInjection;

namespace Panther.Build.Components
{
    [TypeConverter(typeof(TypeConverter<Configuration>))]
    public class Configuration : Enumeration
    {
        public static Configuration Debug = new Configuration { Value = nameof(Debug) };
        public static Configuration Release = new Configuration { Value = nameof(Release) };

        public static implicit operator string(Configuration configuration)
        {
            return configuration.Value;
        }
    }

    public interface IHazConfiguration : INukeBuild
    {
        [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
        Configuration Configuration => ValueInjectionUtility.TryGetValue(() => Configuration) ??
                                                   (IsLocalBuild ? Configuration.Debug : Configuration.Release);
    }
}