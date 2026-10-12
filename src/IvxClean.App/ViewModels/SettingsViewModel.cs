using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvxClean.App.Services;
using IvxClean.Core.Paths;

namespace IvxClean.App.ViewModels;

/// <summary>The Settings screen.</summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly ThemeService _theme;
    private readonly IPathRoots _roots;

    /// <summary>Creates the settings view model from the current settings.</summary>
    public SettingsViewModel(ISettingsService settings, ThemeService theme, IPathRoots roots)
    {
        _settings = settings;
        _theme = theme;
        _roots = roots;

        AppSettings current = settings.Current;
        _selectedTheme = current.Theme;
        _tempMinimumAgeHours = current.TempMinimumAgeHours;
        _quarantineDays = current.QuarantineDays;
        _createRestorePoint = current.CreateRestorePoint;
        _showExpertLevel = current.ShowExpertLevel;
    }

    /// <summary>Theme options for the picker.</summary>
    public AppTheme[] Themes { get; } = [AppTheme.System, AppTheme.Dark, AppTheme.Light];

    [ObservableProperty]
    private AppTheme _selectedTheme;

    [ObservableProperty]
    private int _tempMinimumAgeHours;

    [ObservableProperty]
    private int _quarantineDays;

    [ObservableProperty]
    private bool _createRestorePoint;

    [ObservableProperty]
    private bool _showExpertLevel;

    partial void OnSelectedThemeChanged(AppTheme value)
    {
        _settings.Current.Theme = value;
        _theme.Apply(value);
        _settings.Save();
    }

    partial void OnTempMinimumAgeHoursChanged(int value)
    {
        _settings.Current.TempMinimumAgeHours = Math.Max(1, value);
        _settings.Save();
    }

    partial void OnQuarantineDaysChanged(int value)
    {
        _settings.Current.QuarantineDays = Math.Clamp(value, 1, 30);
        _settings.Save();
    }

    partial void OnCreateRestorePointChanged(bool value)
    {
        _settings.Current.CreateRestorePoint = value;
        _settings.Save();
    }

    partial void OnShowExpertLevelChanged(bool value)
    {
        _settings.Current.ShowExpertLevel = value;
        _settings.Save();
    }

    /// <summary>Opens the IVX Clean logs folder.</summary>
    [RelayCommand]
    private void OpenLogs()
    {
        try
        {
            string dir = System.IO.Path.Combine(_roots.AppDataRoot, "logs");
            System.IO.Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or System.IO.IOException)
        {
            // Convenience only.
        }
    }

    /// <summary>Resets settings to defaults.</summary>
    [RelayCommand]
    private void ResetSettings()
    {
        _settings.Reset();
        SelectedTheme = _settings.Current.Theme;
        TempMinimumAgeHours = _settings.Current.TempMinimumAgeHours;
        QuarantineDays = _settings.Current.QuarantineDays;
        CreateRestorePoint = _settings.Current.CreateRestorePoint;
        ShowExpertLevel = _settings.Current.ShowExpertLevel;
    }
}
