using System.Text.Json.Serialization;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.Settings;

public class ApplicationSettingsStore
{
    [JsonPropertyName("controllers")]
    public ControllerSettings Controllers { get; set; } = new ControllerSettings();

    [JsonPropertyName("debug")]
    public DebuggingSettings Debug { get; set; } = new DebuggingSettings();
}