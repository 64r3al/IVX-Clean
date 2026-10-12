using System;
using System.Security.Principal;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvxClean.App.Services;
using IvxClean.Core;
using Microsoft.Extensions.DependencyInjection;

namespace IvxClean.App.ViewModels;

/// <summary>The shell view model: navigation, sidebar and status.</summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IServiceProvider _services;

    /// <summary>Creates the shell and navigates to the dashboard.</summary>
    public MainWindowViewModel(IServiceProvider services, INavigationService navigation)
    {
        _services = services;
        Navigation = navigation;
        IsAdministrator = DetectAdministrator();
        GoDashboard();
    }

    /// <summary>The navigation service the content area binds to.</summary>
    public INavigationService Navigation { get; }

    /// <summary>Whether the app is running elevated.</summary>
    public bool IsAdministrator { get; }

    /// <summary>The status card text.</summary>
    public string StatusText => IsAdministrator ? "Running as administrator" : "Protection on";

    /// <summary>The sidebar footer version.</summary>
    public string Version => AppInfo.Version;

    [ObservableProperty]
    private string _active = "Dashboard";

    /// <summary>Navigates to the dashboard.</summary>
    [RelayCommand]
    private void GoDashboard()
    {
        Navigation.Show(_services.GetRequiredService<DashboardViewModel>());
        Active = "Dashboard";
    }

    /// <summary>Navigates to the Quarantine screen.</summary>
    [RelayCommand]
    private void GoQuarantine()
    {
        Navigation.Show(_services.GetRequiredService<QuarantineViewModel>());
        Active = "Quarantine";
    }

    /// <summary>Navigates to the Unwanted software screen.</summary>
    [RelayCommand]
    private void GoSoftware()
    {
        Navigation.Show(_services.GetRequiredService<SoftwareViewModel>());
        Active = "Software";
    }

    /// <summary>Navigates to Settings.</summary>
    [RelayCommand]
    private void GoSettings()
    {
        Navigation.Show(_services.GetRequiredService<SettingsViewModel>());
        Active = "Settings";
    }

    /// <summary>Navigates to About.</summary>
    [RelayCommand]
    private void GoAbout()
    {
        Navigation.Show(_services.GetRequiredService<AboutViewModel>());
        Active = "About";
    }

    private static bool DetectAdministrator()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
