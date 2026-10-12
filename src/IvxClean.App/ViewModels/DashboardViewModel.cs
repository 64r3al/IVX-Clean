using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvxClean.App.Services;
using IvxClean.Core;
using Microsoft.Extensions.DependencyInjection;

namespace IvxClean.App.ViewModels;

/// <summary>A selectable cleaning level.</summary>
public sealed record LevelOption(int Number, string Name, string Hint, bool Recommended);

/// <summary>The dashboard / home screen.</summary>
public sealed partial class DashboardViewModel : ObservableObject
{
    private readonly IServiceProvider _services;
    private readonly INavigationService _navigation;
    private readonly ISettingsService _settings;

    /// <summary>Creates the dashboard.</summary>
    public DashboardViewModel(IServiceProvider services, INavigationService navigation, ISettingsService settings)
    {
        _services = services;
        _navigation = navigation;
        _settings = settings;

        Greeting = DateTime.Now.Hour switch
        {
            < 12 => "Good morning",
            < 18 => "Good afternoon",
            _ => "Good evening",
        };

        Levels.Add(new LevelOption(1, "Quick", "Temp files, thumbnails and crash dumps.", false));
        Levels.Add(new LevelOption(2, "Standard", "Quick plus browser, app and GPU caches.", true));
        Levels.Add(new LevelOption(3, "Deep", "Standard plus update cache and app downloads.", false));
        if (settings.Current.ShowExpertLevel)
        {
            Levels.Add(new LevelOption(4, "Expert", "Advanced items — each chosen individually.", false));
        }

        SelectedLevel = Levels.First(l => l.Number == 2);
    }

    /// <summary>The greeting line.</summary>
    public string Greeting { get; }

    /// <summary>Today's date line.</summary>
    public string DateText => DateTime.Now.ToString("dddd, d MMMM");

    /// <summary>The app version for the hero footer.</summary>
    public string Version => AppInfo.Version;

    /// <summary>The selectable levels.</summary>
    public ObservableCollection<LevelOption> Levels { get; } = [];

    [ObservableProperty]
    private LevelOption _selectedLevel;

    /// <summary>Starts a scan at the selected level.</summary>
    [RelayCommand]
    private async Task ScanNowAsync()
    {
        var scan = _services.GetRequiredService<ScanViewModel>();
        _navigation.Show(scan);
        await scan.StartAsync(SelectedLevel.Number, SelectedLevel.Number == 4 && _settings.Current.ShowExpertLevel).ConfigureAwait(true);
    }
}
