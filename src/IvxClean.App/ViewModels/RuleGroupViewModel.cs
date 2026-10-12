using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using IvxClean.Core.Formatting;
using IvxClean.Core.Scanning;

namespace IvxClean.App.ViewModels;

/// <summary>A collapsible category card grouping its reviewable items.</summary>
public sealed partial class RuleGroupViewModel : ObservableObject
{
    /// <summary>Creates a group for a category.</summary>
    public RuleGroupViewModel(string category, IEnumerable<PlanItemViewModel> items)
    {
        Category = category;
        Items = new ObservableCollection<PlanItemViewModel>(items);
    }

    /// <summary>The category name.</summary>
    public string Category { get; }

    /// <summary>The rows in this group.</summary>
    public ObservableCollection<PlanItemViewModel> Items { get; }

    [ObservableProperty]
    private bool _isExpanded = true;

    /// <summary>Includable rows in this group.</summary>
    public IEnumerable<PlanItemViewModel> Includable => Items.Where(i => i.IsIncludable);

    /// <summary>Count of includable rows.</summary>
    public int IncludableCount => Includable.Count();

    /// <summary>Total includable size, formatted.</summary>
    public string SizeText => ByteSize.Humanize(Includable.Sum(i => i.Item.Size));

    /// <summary>Whether all includable rows are selected (for the group "select all" box).</summary>
    public bool? AllSelected
    {
        get
        {
            var flags = Includable.Select(i => i.IsSelected).Distinct().ToList();
            return flags.Count == 0 ? false : flags.Count == 1 ? flags[0] : null;
        }
    }

    /// <summary>Selects or deselects every includable row in the group.</summary>
    public void SetAll(bool selected)
    {
        foreach (PlanItemViewModel item in Includable)
        {
            item.IsSelected = selected;
        }

        OnPropertyChanged(nameof(AllSelected));
    }

    /// <summary>Notifies that a child selection changed (so the header checkbox refreshes).</summary>
    public void RefreshSelectionState() => OnPropertyChanged(nameof(AllSelected));
}
