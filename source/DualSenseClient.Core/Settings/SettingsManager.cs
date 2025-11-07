namespace DualSenseClient.Core.Settings;

public interface ISettingsManager : IDisposable
{
    ApplicationSettingsStore Application { get; }
    event EventHandler<ApplicationSettingsStore>? SettingsChanged;
    void SaveAll();
    void ReloadAll();
}

public class SettingsManager : ISettingsManager
{
    private readonly IApplicationSettings _appSettings;

    public event EventHandler<ApplicationSettingsStore>? SettingsChanged;

    public SettingsManager(IApplicationSettings appSettings)
    {
        _appSettings = appSettings;
        _appSettings.Changed += (_, s) => SettingsChanged?.Invoke(this, s);
    }

    public ApplicationSettingsStore Application => _appSettings.Current;

    public void SaveAll() => _appSettings.Save();
    public void ReloadAll() => _appSettings.Reload();

    public void Dispose() => _appSettings.Changed -= (_, s) => SettingsChanged?.Invoke(this, s);
}