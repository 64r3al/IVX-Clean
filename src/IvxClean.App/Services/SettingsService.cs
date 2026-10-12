using System;
using System.IO;
using System.Text.Json;
using IvxClean.Core.Paths;

namespace IvxClean.App.Services;

/// <summary>Loads and saves <see cref="AppSettings"/>.</summary>
public interface ISettingsService
{
    /// <summary>The current settings (never null).</summary>
    AppSettings Current { get; }

    /// <summary>Persists the current settings to disk.</summary>
    void Save();

    /// <summary>Resets settings to defaults and persists.</summary>
    void Reset();
}

/// <inheritdoc cref="ISettingsService"/>
public sealed class SettingsService : ISettingsService
{
    private readonly string _path;

    /// <summary>Creates the service and loads existing settings if present.</summary>
    public SettingsService(IPathRoots roots)
    {
        ArgumentNullException.ThrowIfNull(roots);
        Directory.CreateDirectory(roots.AppDataRoot);
        _path = Path.Combine(roots.AppDataRoot, "settings.json");
        Current = Load();
    }

    /// <inheritdoc />
    public AppSettings Current { get; private set; }

    /// <inheritdoc />
    public void Save()
    {
        try
        {
            File.WriteAllText(_path, JsonSerializer.Serialize(Current, SettingsJsonContext.Default.AppSettings));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // A failed save must not crash the app; the in-memory settings still apply this session.
        }
    }

    /// <inheritdoc />
    public void Reset()
    {
        Current = new AppSettings();
        Save();
    }

    private AppSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                return JsonSerializer.Deserialize(File.ReadAllText(_path), SettingsJsonContext.Default.AppSettings) ?? new AppSettings();
            }
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // Corrupt or unreadable settings fall back to defaults.
        }

        return new AppSettings();
    }
}
