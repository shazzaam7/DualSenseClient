using System.Text.Json.Serialization;

namespace DualSenseClient.Core.Settings.Models;

public class UiSettings
{
    [JsonPropertyName("theme")]
    public AppTheme Theme { get; set; } = AppTheme.Dark;
}

public enum AppTheme
{
    Light,
    Dark
}