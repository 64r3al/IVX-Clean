using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvxClean.App.Services;
using IvxClean.Core.Formatting;
using IvxClean.Core.Software;

namespace IvxClean.App.ViewModels;

/// <summary>A software row in the Unwanted software screen.</summary>
public sealed partial class SoftwareRow(SoftwareEntry entry) : ObservableObject
{
    /// <summary>The underlying classified entry.</summary>
    public SoftwareEntry Entry { get; } = entry;

    /// <summary>Display name.</summary>
    public string Name => Entry.DisplayName;

    /// <summary>Publisher.</summary>
    public string Publisher => string.IsNullOrWhiteSpace(Entry.Publisher) ? "Unknown publisher" : Entry.Publisher;

    /// <summary>Size, formatted.</summary>
    public string SizeText => Entry.Raw.EstimatedSizeBytes > 0 ? ByteSize.Humanize(Entry.Raw.EstimatedSizeBytes) : string.Empty;

    [ObservableProperty]
    private bool _isBusy;
}

/// <summary>The Unwanted software screen.</summary>
public sealed partial class SoftwareViewModel : ObservableObject
{
    private readonly SoftwareInventory _inventory;
    private readonly ISoftwareUninstaller _uninstaller;
    private readonly IDialogService _dialogs;

    /// <summary>Creates the software view model.</summary>
    public SoftwareViewModel(SoftwareInventory inventory, ISoftwareUninstaller uninstaller, IDialogService dialogs)
    {
        _inventory = inventory;
        _uninstaller = uninstaller;
        _dialogs = dialogs;
        _ = LoadAsync();
    }

    [ObservableProperty]
    private bool _isLoading = true;

    /// <summary>Known bloatware.</summary>
    public ObservableCollection<SoftwareRow> KnownBloatware { get; } = [];

    /// <summary>Maybe unwanted.</summary>
    public ObservableCollection<SoftwareRow> MaybeUnwanted { get; } = [];

    /// <summary>Kept apps.</summary>
    public ObservableCollection<SoftwareRow> Keep { get; } = [];

    /// <summary>Loads and classifies the inventory off the UI thread.</summary>
    public async Task LoadAsync()
    {
        IsLoading = true;
        SoftwareInventoryResult result = await Task.Run(_inventory.Build).ConfigureAwait(true);

        Fill(KnownBloatware, result.KnownBloatware);
        Fill(MaybeUnwanted, result.MaybeUnwanted);
        Fill(Keep, result.Keep);
        IsLoading = false;

        static void Fill(ObservableCollection<SoftwareRow> target, System.Collections.Generic.IReadOnlyList<SoftwareEntry> source)
        {
            target.Clear();
            foreach (SoftwareEntry e in source)
            {
                target.Add(new SoftwareRow(e));
            }
        }
    }

    /// <summary>Uninstalls a single app after confirmation, then refreshes.</summary>
    [RelayCommand]
    private async Task UninstallAsync(SoftwareRow row)
    {
        bool ok = await _dialogs.ConfirmAsync(
            $"Uninstall {row.Name}?",
            "This runs the app's own uninstaller. Nothing else is removed.",
            "Uninstall…", "Cancel").ConfigureAwait(true);
        if (!ok)
        {
            return;
        }

        row.IsBusy = true;
        UninstallResult result = await _uninstaller.UninstallAsync(row.Entry, System.Threading.CancellationToken.None).ConfigureAwait(true);
        row.IsBusy = false;
        await _dialogs.InfoAsync(row.Name, result.Message).ConfigureAwait(true);
        await LoadAsync().ConfigureAwait(true);
    }
}
