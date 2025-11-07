using System.Text.Json;
using System.Text.Json.Serialization;
using DualSenseClient.Core.Logging;

namespace DualSenseClient.Core.Settings;

public abstract class JsonSettingsService<T> : ISettingsService<T> where T : class, new()
{
    private readonly string _path;
    private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
    private T _current;

    private readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        Converters = { new JsonStringEnumConverter() },
        WriteIndented = true
    };

    protected virtual T Default => new T();

    public event EventHandler<T>? Changed;

    public T Current
    {
        get
        {
            _lock.EnterReadLock();
            try
            {
                return _current;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
    }

    protected JsonSettingsService(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentNullException(nameof(filePath));
        }

        _path = filePath;

        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        _current = LoadInternal();
    }

    public void Save() => Save(_current);

    public void Save(T newSettings)
    {
        ArgumentNullException.ThrowIfNull(newSettings);

        _lock.EnterWriteLock();
        try
        {
            string tmp = _path + ".tmp";
            string json = JsonSerializer.Serialize(newSettings, Options);

            File.WriteAllText(tmp, json);
            File.Move(tmp, _path, true);

            _current = newSettings;
        }
        catch (Exception ex)
        {
            LogError($"Failed to save settings to {_path}", ex);
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        Changed?.Invoke(this, _current);
    }

    public void Reload()
    {
        _lock.EnterWriteLock();
        try
        {
            _current = LoadInternal();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        Changed?.Invoke(this, _current);
    }

    private T LoadInternal()
    {
        T defaultSettings;
        try
        {
            if (!File.Exists(_path))
            {
                defaultSettings = Default;
                SaveInternal(defaultSettings);
                return defaultSettings;
            }

            string fileJson = File.ReadAllText(_path);

            // Parse file and default settings
            using JsonDocument fileDoc = JsonDocument.Parse(fileJson);
            defaultSettings = Default;
            string defaultJson = JsonSerializer.Serialize(defaultSettings, Options);
            using JsonDocument defaultDoc = JsonDocument.Parse(defaultJson);

            // Merge file and defaults
            JsonElement merged = MergeJsonElements(defaultDoc.RootElement, fileDoc.RootElement);
            string mergedJson = merged.GetRawText();

            // Check if merged result differ from the file (normalized)
            string normalizedFile = NormalizeJson(fileJson);
            string normalizedMerged = NormalizeJson(mergedJson);

            T result = JsonSerializer.Deserialize<T>(mergedJson, Options) ?? defaultSettings;

            // Force save to file if they differ
            if (normalizedFile != normalizedMerged)
            {
                SaveInternal(result);
            }

            return result;
        }
        catch (Exception ex)
        {
            LogError($"Failed to load settings from {_path}", ex);
            defaultSettings = Default;
            SaveInternal(defaultSettings);
            return defaultSettings;
        }
    }

    private string NormalizeJson(string json)
    {
        // Parse, normalize & re-serialize
        using JsonDocument doc = JsonDocument.Parse(json);
        return JsonSerializer.Serialize(doc.RootElement);
    }

    private JsonElement MergeJsonElements(JsonElement defaultElement, JsonElement fileElement)
    {
        if (defaultElement.ValueKind != JsonValueKind.Object)
        {
            return fileElement.ValueKind != JsonValueKind.Undefined ? fileElement : defaultElement;
        }

        using MemoryStream stream = new MemoryStream();
        using Utf8JsonWriter writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });

        writer.WriteStartObject();

        // Track which properties we've already written
        HashSet<string> writtenProperties = [];

        // Add all properties from default, merging with file where applicable
        foreach (JsonProperty defaultProp in defaultElement.EnumerateObject())
        {
            writer.WritePropertyName(defaultProp.Name);
            writtenProperties.Add(defaultProp.Name);

            if (fileElement.TryGetProperty(defaultProp.Name, out var fileProp))
            {
                if (defaultProp.Value.ValueKind == JsonValueKind.Object && fileProp.ValueKind == JsonValueKind.Object)
                {
                    // Recursively merge nested objects
                    JsonElement merged = MergeJsonElements(defaultProp.Value, fileProp);
                    merged.WriteTo(writer);
                }
                else
                {
                    // Use value from file
                    fileProp.WriteTo(writer);
                }
            }
            else
            {
                // Setting missing in the file, use default
                defaultProp.Value.WriteTo(writer);
            }
        }

        // Add properties from file that don't exist in default
        foreach (JsonProperty fileProp in fileElement.EnumerateObject())
        {
            if (!writtenProperties.Contains(fileProp.Name))
            {
                writer.WritePropertyName(fileProp.Name);
                fileProp.Value.WriteTo(writer);
            }
        }

        writer.WriteEndObject();
        writer.Flush();

        stream.Position = 0;
        return JsonDocument.Parse(stream).RootElement.Clone();
    }

    private void SaveInternal(T settings)
    {
        try
        {
            string tmp = _path + ".tmp";
            string json = JsonSerializer.Serialize(settings, Options);

            File.WriteAllText(tmp, json);
            File.Move(tmp, _path, true);
        }
        catch (Exception ex)
        {
            LogError($"Failed to save settings to {_path}", ex);
        }
    }

    protected virtual void LogError(string message, Exception ex)
    {
        Logger.Error(message);
        Logger.LogExceptionDetails(ex);
    }
}