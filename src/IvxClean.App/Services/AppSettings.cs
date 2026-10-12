using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace IvxClean.App.Services;

/// <summary>The app's theme preference.</summary>
public enum AppTheme
{
    /// <summary>Follow the Windows light/dark setting.</summary>
    System,

    /// <summary>Always dark.</summary>
    Dark,

    /// <summary>Always light.</summary>
    Light,
}

/// <summary>User-configurable settings, persisted to <c>%LOCALAPPDATA%\IVXClean\settings.json</c>.</summary>
public sealed class AppSettings
{
    /// <summary>The theme preference.</summary>
    public AppTheme Theme { get; set; } = AppTheme.System;

    /// <summary>UI language code ("en" or "fr").</summary>
    public string Language { get; set; } = "en";

    /// <summary>Folders the user marked protected (never cleaned).</summary>
    public List<string> ProtectedFolders { get; set; } = [];

    /// <summary>Rule ids the user has excluded.</summary>
    public List<string> ExcludedRuleIds { get; set; } = [];

    /// <summary>Minimum age (hours) before a temp file is eligible. Minimum 1.</summary>
    public int TempMinimumAgeHours { get; set; } = 24;

    /// <summary>Days quarantined items are kept before auto-purge (1–30).</summary>
    public int QuarantineDays { get; set; } = 7;

    /// <summary>Whether to create a System Restore point before cleaning (on by default).</summary>
    public bool CreateRestorePoint { get; set; } = true;

    /// <summary>Whether the Expert level is shown (off by default).</summary>
    public bool ShowExpertLevel { get; set; }
}

/// <summary>Source-generated JSON for settings.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true, UseStringEnumConverter = true)]
[JsonSerializable(typeof(AppSettings))]
public sealed partial class SettingsJsonContext : JsonSerializerContext;
