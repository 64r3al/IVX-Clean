using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvxClean.App.Services;
using IvxClean.Core.Cleaning;
using IvxClean.Core.Formatting;
using IvxClean.Core.Rules;
using IvxClean.Core.Scanning;
using IvxClean.Core.SystemOps;

namespace IvxClean.App.ViewModels;

/// <summary>The risk filter on the Review screen.</summary>
public enum ReviewFilter
{
    /// <summary>Show everything.</summary>
    All,

    /// <summary>Only Safe items.</summary>
    Safe,

    /// <summary>Only Caution items.</summary>
    Caution,

    /// <summary>Only Expert items.</summary>
    Expert,
}

/// <summary>The "Review before cleaning" screen — the most important screen in the app.</summary>
public sealed partial class ReviewViewModel : ObservableObject
{
    private readonly CleanPlan _plan;
    private readonly ReviewServices? _services;
    private readonly List<PlanItemViewModel> _allItems = [];

    /// <summary>Services the Review screen needs to actually clean (null in logic-only tests).</summary>
    public sealed record ReviewServices(
        ICleaner Cleaner,
        IRestorePointService RestorePoints,
        IServicesManager Services,
        ISettingsService Settings,
        INavigationService Navigation,
        IDialogService Dialogs);

    /// <summary>Creates the Review view model for a scan plan.</summary>
    public ReviewViewModel(CleanPlan plan, ReviewServices? services = null)
    {
        _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        _services = services;
        _createRestorePoint = services?.Settings.Current.CreateRestorePoint ?? true;

        foreach (IGrouping<string, PlanItem> group in plan.Items.GroupBy(i => i.Category).OrderBy(g => g.Key, StringComparer.CurrentCulture))
        {
            var rows = group
                .OrderByDescending(i => i.Size)
                .Select(i => new PlanItemViewModel(i, OnItemSelectionChanged))
                .ToList();
            _allItems.AddRange(rows);
            Groups.Add(new RuleGroupViewModel(group.Key, rows));
        }

        RecomputeTotals();
        ApplyFilter();
    }

    /// <summary>The category groups.</summary>
    public ObservableCollection<RuleGroupViewModel> Groups { get; } = [];

    /// <summary>Every row (includable, protected and skipped), flattened.</summary>
    public IReadOnlyList<PlanItemViewModel> AllItems => _allItems;

    private ICollectionView? _itemsView;

    /// <summary>A grouped, filtered, virtualizable view of all rows for the list (built lazily on the UI thread).</summary>
    public ICollectionView ItemsView
    {
        get
        {
            if (_itemsView is null)
            {
                var source = new CollectionViewSource { Source = _allItems };
                source.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PlanItemViewModel.Category)));
                _itemsView = source.View;
                _itemsView.Filter = PassesFilter;
            }

            return _itemsView;
        }
    }

    /// <summary>The available risk filters (for the segmented control).</summary>
    public ReviewFilter[] Filters { get; } = [ReviewFilter.All, ReviewFilter.Safe, ReviewFilter.Caution, ReviewFilter.Expert];

    [ObservableProperty]
    private ReviewFilter _filter = ReviewFilter.All;

    [ObservableProperty]
    private bool _createRestorePoint;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private long _selectedBytes;

    [ObservableProperty]
    private int _selectedCount;

    [ObservableProperty]
    private long _deleteBytes;

    [ObservableProperty]
    private long _quarantineBytes;

    /// <summary>Whether at least one item is selected (enables the Clean button).</summary>
    public bool HasSelection => SelectedCount > 0;

    /// <summary>Total includable items found.</summary>
    public int FoundCount => _allItems.Count(i => i.IsIncludable);

    /// <summary>Count of protected + skipped items.</summary>
    public int SkippedOrProtectedCount => _plan.ProtectedCount + _plan.SkippedCount;

    /// <summary>The primary button label, e.g. "Clean 7.0 GB".</summary>
    public string CleanButtonText => $"Clean {ByteSize.Humanize(SelectedBytes)}";

    /// <summary>Human text for the deleted-vs-quarantined split.</summary>
    public string SplitText => $"{ByteSize.Humanize(DeleteBytes)} deleted · {ByteSize.Humanize(QuarantineBytes)} to Quarantine";

    partial void OnFilterChanged(ReviewFilter value)
    {
        ApplyFilter();
        _itemsView?.Refresh();
    }

    private bool PassesFilter(object obj)
    {
        if (obj is not PlanItemViewModel item)
        {
            return true;
        }

        return Filter switch
        {
            ReviewFilter.Safe => item.Item.Risk == RuleRisk.Safe,
            ReviewFilter.Caution => item.Item.Risk == RuleRisk.Caution,
            ReviewFilter.Expert => item.Item.Risk == RuleRisk.Expert,
            _ => true,
        };
    }

    /// <summary>Selects or clears every includable row in the named category group.</summary>
    [RelayCommand]
    private void ToggleGroup(string category)
    {
        RuleGroupViewModel? group = Groups.FirstOrDefault(g => string.Equals(g.Category, category, StringComparison.Ordinal));
        if (group is not null)
        {
            group.SetAll(group.AllSelected != true);
        }
    }

    /// <summary>Exports the selected file list as text.</summary>
    [RelayCommand]
    private void ExportList()
    {
        if (_services is null)
        {
            return;
        }

        string? path = _services.Dialogs.AskSaveTextPath("ivxclean-selected-files.txt");
        if (path is null)
        {
            return;
        }

        var sb = new StringBuilder();
        foreach (PlanItemViewModel item in _allItems.Where(i => i.IsIncludable && i.IsSelected))
        {
            sb.AppendLine(item.FullPath);
        }

        File.WriteAllText(path, sb.ToString());
    }

    /// <summary>Confirms and performs the clean.</summary>
    [RelayCommand]
    private async Task CleanAsync()
    {
        if (_services is null || IsBusy)
        {
            return;
        }

        List<PlanItem> selected = _allItems.Where(i => i.IsIncludable && i.IsSelected).Select(i => i.Item).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        bool confirmed = await _services.Dialogs.ConfirmAsync(
            $"Clean {ByteSize.Humanize(SelectedBytes)} from {selected.Select(i => i.BaseFolder).Distinct().Count()} locations?",
            "Only the items you selected will be removed. Your files, games and drivers stay exactly as they are.",
            CleanButtonText,
            "Back to review").ConfigureAwait(true);
        if (!confirmed)
        {
            return;
        }

        IsBusy = true;
        try
        {
            if (CreateRestorePoint)
            {
                RestorePointOutcome outcome = _services.RestorePoints.CreateRestorePoint("Before IVX Clean");
                if (outcome is RestorePointOutcome.ProtectionDisabled or RestorePointOutcome.Failed)
                {
                    bool keepGoing = await _services.Dialogs.ConfirmAsync(
                        "Could not create a restore point",
                        "System Protection may be turned off. You can continue without a restore point, or cancel and turn it on in Windows.",
                        "Continue without", "Cancel").ConfigureAwait(true);
                    if (!keepGoing)
                    {
                        return;
                    }
                }
            }

            var cleaning = new CleaningViewModel();
            _services.Navigation.Show(cleaning);
            var progress = new Progress<CleanProgress>(cleaning.Update);

            CleanReport report = await RunCleanAsync(selected, progress).ConfigureAwait(true);
            _services.Navigation.Show(new ReportViewModel(report, _services.Navigation));
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<CleanReport> RunCleanAsync(List<PlanItem> selected, IProgress<CleanProgress> progress)
    {
        CleanReport? report = null;
        var options = new CleanOptions { Description = $"Level {_plan.Level} clean" };

        async Task DoCleanAsync(CancellationToken ct)
            => report = await _services!.Cleaner.CleanAsync(selected, options, progress, ct).ConfigureAwait(false);

        bool needsUpdateServices = selected.Any(i => string.Equals(i.Category, "Windows Update", StringComparison.Ordinal));
        if (needsUpdateServices)
        {
            await _services!.Services.RunWithServicesStoppedAsync(["wuauserv", "bits", "dosvc"], DoCleanAsync, CancellationToken.None).ConfigureAwait(true);
        }
        else
        {
            await DoCleanAsync(CancellationToken.None).ConfigureAwait(true);
        }

        return report!;
    }

    private void OnItemSelectionChanged()
    {
        RecomputeTotals();
        foreach (RuleGroupViewModel group in Groups)
        {
            group.RefreshSelectionState();
        }
    }

    private void RecomputeTotals()
    {
        List<PlanItemViewModel> selected = _allItems.Where(i => i.IsIncludable && i.IsSelected).ToList();
        SelectedCount = selected.Count;
        SelectedBytes = selected.Sum(i => i.Item.Size);
        DeleteBytes = selected.Where(i => i.Item.Action == RuleAction.Delete).Sum(i => i.Item.Size);
        QuarantineBytes = selected.Where(i => i.Item.Action == RuleAction.Quarantine).Sum(i => i.Item.Size);
        OnPropertyChanged(nameof(CleanButtonText));
        OnPropertyChanged(nameof(SplitText));
        OnPropertyChanged(nameof(FoundCount));
        OnPropertyChanged(nameof(HasSelection));
    }

    private void ApplyFilter()
    {
        foreach (PlanItemViewModel item in _allItems)
        {
            item.IsVisible = Filter switch
            {
                ReviewFilter.All => true,
                ReviewFilter.Safe => item.Item.Risk == RuleRisk.Safe,
                ReviewFilter.Caution => item.Item.Risk == RuleRisk.Caution,
                ReviewFilter.Expert => item.Item.Risk == RuleRisk.Expert,
                _ => true,
            };
        }
    }
}
