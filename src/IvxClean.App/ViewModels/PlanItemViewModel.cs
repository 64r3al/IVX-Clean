using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using IvxClean.Core.Formatting;
using IvxClean.Core.Rules;
using IvxClean.Core.Scanning;

namespace IvxClean.App.ViewModels;

/// <summary>A single reviewable item row.</summary>
public sealed partial class PlanItemViewModel : ObservableObject
{
    private readonly Action _onSelectionChanged;

    /// <summary>Creates a row for a scanned item.</summary>
    public PlanItemViewModel(PlanItem item, Action onSelectionChanged)
    {
        Item = item;
        _onSelectionChanged = onSelectionChanged;
        _isSelected = item.Decision == PlanDecision.Include;
    }

    /// <summary>The underlying scanned item.</summary>
    public PlanItem Item { get; }

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private bool _isWhyExpanded;

    [ObservableProperty]
    private bool _isVisible = true;

    /// <summary>Whether this item can be cleaned (and therefore toggled).</summary>
    public bool IsIncludable => Item.Decision == PlanDecision.Include;

    /// <summary>The leaf file/folder name.</summary>
    public string Name => Path.GetFileName(Item.Path.TrimEnd('\\', '/')) is { Length: > 0 } n ? n : Item.Path;

    /// <summary>The full path (mono, ellipsised in the UI; full on tooltip).</summary>
    public string FullPath => Item.Path;

    /// <summary>The item size, formatted.</summary>
    public string SizeText => Item.IsDirectory ? string.Empty : ByteSize.Humanize(Item.Size);

    /// <summary>The risk badge text.</summary>
    public string RiskText => Item.Risk switch
    {
        RuleRisk.Safe => "Safe",
        RuleRisk.Caution => "Caution",
        _ => "Expert",
    };

    /// <summary>The decision (Include/Protected/Skipped).</summary>
    public PlanDecision Decision => Item.Decision;

    /// <summary>The reason/explanation shown for protected and skipped items, and in the ⓘ panel.</summary>
    public string Reason => Item.Reason;

    partial void OnIsSelectedChanged(bool value) => _onSelectionChanged();
}
