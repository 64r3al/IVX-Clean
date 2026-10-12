using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvxClean.Core;

namespace IvxClean.App.ViewModels;

/// <summary>The About screen.</summary>
public sealed partial class AboutViewModel : ObservableObject
{
    /// <summary>The version line, e.g. "Version 1.0.0 · Made by IVX".</summary>
    public string VersionLine => $"Version {AppInfo.Version} · Made by {AppInfo.MadeBy}";

    /// <summary>The promise checklist.</summary>
    public string[] Promises { get; } =
    [
        "No registry cleaning",
        "No fake speed boosts",
        "No ads, no tracking",
        "Never touches your files",
        "Never touches drivers",
        "Undo for 7 days",
    ];

    /// <summary>Opens the GitHub repository.</summary>
    [RelayCommand]
    private void ViewOnGitHub() => OpenUrl(AppInfo.RepositoryUrl);

    /// <summary>Opens the latest release page (user-initiated update check).</summary>
    [RelayCommand]
    private void CheckForUpdates() => OpenUrl(AppInfo.RepositoryUrl + "/releases/latest");

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // Ignore; opening a browser is a convenience.
        }
    }
}
