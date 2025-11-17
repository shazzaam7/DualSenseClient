using System.Text.Json.Serialization;
using DualSenseClient.Core.DualSense.Enums;

namespace DualSenseClient.Core.Settings.Models;

public class ControllerSettings
{
    [JsonPropertyName("knownControllers")]
    public Dictionary<string, ControllerInfo> KnownControllers { get; set; } = new Dictionary<string, ControllerInfo>();

    [JsonPropertyName("profiles")]
    public Dictionary<string, ControllerProfile> Profiles { get; set; } = new Dictionary<string, ControllerProfile>();

    [JsonPropertyName("defaultProfileId")]
    public string? DefaultProfileId { get; set; }
}

public class ControllerInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = "DualSense Controller";

    [JsonPropertyName("macAddress")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("serialNumber")]
    public string? SerialNumber { get; set; }

    [JsonPropertyName("profileId")]
    public string? ProfileId { get; set; }

    [JsonPropertyName("lastSeen")]
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("connectionType")]
    public ConnectionType? LastConnectionType { get; set; }
}

public class ControllerProfile
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = "Default Profile";

    [JsonPropertyName("lightbar")]
    public LightbarSettings Lightbar { get; set; } = new();

    [JsonPropertyName("playerLeds")]
    public PlayerLedSettings PlayerLeds { get; set; } = new();

    [JsonPropertyName("micLed")]
    public MicLed MicLed { get; set; } = MicLed.Off;

    [JsonPropertyName("special_actions")]
    public List<SpecialActionSettings> SpecialActions { get; set; } = new List<SpecialActionSettings>();
}

public class LightbarSettings
{
    [JsonPropertyName("behavior")]
    public LightbarBehavior Behavior { get; set; } = LightbarBehavior.Custom;

    [JsonPropertyName("red")]
    public byte Red { get; set; } = 0;

    [JsonPropertyName("green")]
    public byte Green { get; set; } = 0;

    [JsonPropertyName("blue")]
    public byte Blue { get; set; } = 255;
}

public class PlayerLedSettings
{
    [JsonPropertyName("pattern")]
    public PlayerLed Pattern { get; set; } = PlayerLed.LED_1;

    [JsonPropertyName("brightness")]
    public PlayerLedBrightness Brightness { get; set; } = PlayerLedBrightness.High;
}