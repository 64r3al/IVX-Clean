using CommunityToolkit.Mvvm.ComponentModel;
using IvxClean.Core.Cleaning;

namespace IvxClean.App.ViewModels;

/// <summary>Shown while a clean runs, with real per-item progress.</summary>
public sealed partial class CleaningViewModel : ObservableObject
{
    [ObservableProperty]
    private double _percent;

    [ObservableProperty]
    private int _itemsProcessed;

    [ObservableProperty]
    private int _itemsTotal;

    [ObservableProperty]
    private string _currentCategory = string.Empty;

    [ObservableProperty]
    private string _currentPath = string.Empty;

    /// <summary>Applies a progress update from the cleaner (called on the UI thread).</summary>
    public void Update(CleanProgress p)
    {
        ItemsTotal = p.ItemsTotal;
        ItemsProcessed = p.ItemsProcessed;
        Percent = p.ItemsTotal == 0 ? 0 : 100.0 * p.ItemsProcessed / p.ItemsTotal;
        CurrentCategory = p.Category;
        CurrentPath = p.CurrentPath;
    }
}
