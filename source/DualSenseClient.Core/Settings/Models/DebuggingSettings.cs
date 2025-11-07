using System.Text.Json.Serialization;

namespace DualSenseClient.Core.Settings.Models;

public class DebuggingSettings
{
    [JsonPropertyName("logging")]
    public Logging Logger { get; set; } = new Logging();

    public class Logging
    {
        [JsonPropertyName("level")]
        public string Level { get; set; } = "Info";
    }
}