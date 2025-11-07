using System.Text.Json.Serialization;
using DualSenseClient.Core.Settings.Models;

namespace DualSenseClient.Core.Settings;

public class ApplicationSettingsStore
{
    [JsonPropertyName("debug")]
    public DebuggingSettings Debug { get; set; } = new DebuggingSettings();
}