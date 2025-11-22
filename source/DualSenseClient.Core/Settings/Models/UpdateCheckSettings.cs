using System.Text.Json.Serialization;

namespace DualSenseClient.Core.Settings.Models;

/// <summary>
/// Subsection for update checks
/// </summary>
public class UpdateCheckSettings
{
    [JsonPropertyName("nightly_build")]
#if DEBUG
    public bool NightlyBuild { get; set; } = true;
#else
    public bool NightlyBuild { get; set; } = false;
#endif

    [JsonPropertyName("last_manager_update_check")]
    public DateTime LastUpdateCheck { get; set; } = DateTime.Now;
}