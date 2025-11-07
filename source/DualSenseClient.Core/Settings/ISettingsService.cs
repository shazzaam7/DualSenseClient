namespace DualSenseClient.Core.Settings;

public interface ISettingsService<T> where T : class, new()
{
    // Current settings
    T Current { get; }

    // Event raised when settings are saved/reloaded
    event EventHandler<T>? Changed;

    // Save current settings to a file
    void Save();

    // Overwrite settings and save them to a file
    void Save(T newSettings);

    // Reload settings from disk
    void Reload();
}