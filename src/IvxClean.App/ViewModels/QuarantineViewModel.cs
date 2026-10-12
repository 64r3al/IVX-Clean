using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvxClean.App.Services;
using IvxClean.Core.Formatting;
using IvxClean.Core.Quarantine;

namespace IvxClean.App.ViewModels;

/// <summary>A quarantine run row.</summary>
public sealed record QuarantineRunRow(string RunId, string Description, string DateText, string SizeText, int DaysLeft, int ItemCount);

/// <summary>The Quarantine screen.</summary>
public sealed partial class QuarantineViewModel : ObservableObject
{
    private readonly IQuarantineStore _store;
    private readonly IDialogService _dialogs;

    /// <summary>Creates the Quarantine view model and loads the runs.</summary>
    public QuarantineViewModel(IQuarantineStore store, IDialogService dialogs)
    {
        _store = store;
        _dialogs = dialogs;
        Reload();
    }

    /// <summary>The quarantine runs, newest first.</summary>
    public ObservableCollection<QuarantineRunRow> Runs { get; } = [];

    /// <summary>Whether there are no runs.</summary>
    public bool IsEmpty => Runs.Count == 0;

    /// <summary>Restores a run to its original locations.</summary>
    [RelayCommand]
    private async Task RestoreAsync(QuarantineRunRow row)
    {
        RestoreResult result = _store.Restore(row.RunId, path => RestoreConflictResolution.KeepBoth);
        await _dialogs.InfoAsync("Restore finished", $"Restored {result.Restored} item(s). Skipped {result.Skipped}, failed {result.Failed}.").ConfigureAwait(true);
        Reload();
    }

    /// <summary>Permanently deletes a run after confirmation.</summary>
    [RelayCommand]
    private async Task DeleteNowAsync(QuarantineRunRow row)
    {
        bool ok = await _dialogs.ConfirmAsync(
            "Delete these items for good?",
            "This permanently removes the quarantined items. It cannot be undone.",
            "Delete now", "Keep").ConfigureAwait(true);
        if (ok)
        {
            _store.DeleteRun(row.RunId);
            Reload();
        }
    }

    private void Reload()
    {
        Runs.Clear();
        foreach (QuarantineRun run in _store.ListRuns())
        {
            Runs.Add(new QuarantineRunRow(
                run.RunId,
                string.IsNullOrWhiteSpace(run.Description) ? "Clean" : run.Description,
                run.CreatedUtc.ToLocalTime().ToString("d MMM yyyy, HH:mm"),
                ByteSize.Humanize(run.TotalBytes),
                run.DaysLeft,
                run.ItemCount));
        }

        OnPropertyChanged(nameof(IsEmpty));
    }
}
