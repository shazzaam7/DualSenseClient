using DualSenseClient.Core.Paths;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.Settings;

public interface IApplicationSettings : ISettingsService<ApplicationSettingsStore>
{
}

public class ApplicationSettings() : JsonSettingsService<ApplicationSettingsStore>(PathResolver.ConfigFile), IApplicationSettings
{
    protected override ApplicationSettingsStore Default => new ApplicationSettingsStore
    {
        Controllers = new ControllerSettings
        {
            KnownControllers = new Dictionary<string, ControllerInfo>(),
            Profiles = new Dictionary<string, ControllerProfile>(),
            DefaultProfileId = null
        },
        Debug = new DebuggingSettings
        {
            Logger = new DebuggingSettings.Logging
            {
                Level = "Error"
            }
        }
    };
}