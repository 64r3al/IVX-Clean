using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvxClean.App.Services;
using IvxClean.Core.Cleaning;
using IvxClean.Core.Formatting;
using IvxClean.Core.Scanning;
using IvxClean.Core.SystemOps;

namespace IvxClean.App.ViewModels;

/// <summary>The scanning screen. Read-only; nothing is deleted.</summary>
public sealed partial class ScanViewModel : ObservableObject, IDisposable
{
    private readonly IScanner _scanner;
    private readonly INavigationService _navigation;
    private readonly ReviewViewModel.ReviewServices _reviewServices;
    private CancellationTokenSource? _cts;

    /// <summary>Creates the scan view model.</summary>
    public ScanViewModel(
        IScanner scanner,
        INavigationService navigation,
        ICleaner cleaner,
        IRestorePointService restorePoints,
        IServicesManager services,
        ISettingsService settings,
        IDialogService dialogs)
    {
        _scanner = scanner;
        _navigation = navigation;
        _reviewServices = new ReviewViewModel.ReviewServices(cleaner, restorePoints, services, settings, navigation, dialogs);
    }

    [ObservableProperty]
    private double _percent;

    [ObservableProperty]
    private string _currentPath = string.Empty;

    [ObservableProperty]
    private string _foundText = "0 bytes";

    [ObservableProperty]
    private bool _isScanning;

    /// <summary>The live per-category list.</summary>
    public ObservableCollection<string> Categories { get; } = [];

    /// <summary>Starts a scan at the given level. Progress marshals to the UI thread.</summary>
    public async Task StartAsync(int level, bool includeExpert)
    {
        _cts = new CancellationTokenSource();
        IsScanning = true;
        var progress = new Progress<ScanProgress>(p =>
        {
            Percent = p.RulesTotal == 0 ? 0 : 100.0 * p.RulesCompleted / p.RulesTotal;
            CurrentPath = p.CurrentPath;
            FoundText = ByteSize.Humanize(p.BytesFound);
        });

        try
        {
            AppLog.Info($"Scan started (level {level}, expert {includeExpert}).");
            CleanPlan plan = await _scanner.ScanAsync(level, includeExpert, progress, _cts.Token).ConfigureAwait(true);
            IsScanning = false;
            AppLog.Info($"Scan finished: {plan.Includable.Count()} includable items, {ByteSize.Humanize(plan.IncludableBytes)}.");
            _navigation.Show(new ReviewViewModel(plan, _reviewServices));
        }
        catch (OperationCanceledException)
        {
            IsScanning = false;
        }
        catch (Exception ex)
        {
            IsScanning = false;
            AppLog.Error(ex, "Scan failed");
            throw;
        }
    }

    /// <summary>Cancels the scan in progress.</summary>
    [RelayCommand]
    private void Cancel() => _cts?.Cancel();

    /// <summary>Disposes the cancellation source.</summary>
    public void Dispose() => _cts?.Dispose();
}
