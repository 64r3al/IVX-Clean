using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using IvxClean.App.Services;
using IvxClean.Core.Cleaning;
using IvxClean.Core.Formatting;

namespace IvxClean.App.ViewModels;

/// <summary>A single category's freed-space bar on the report.</summary>
public sealed record ReportCategory(string Category, string SizeText, long Bytes);

/// <summary>The report screen shown after a clean finishes.</summary>
public sealed partial class ReportViewModel : ObservableObject
{
    private readonly CleanReport _report;
    private readonly INavigationService _navigation;

    /// <summary>Creates the report view model.</summary>
    public ReportViewModel(CleanReport report, INavigationService navigation)
    {
        _report = report ?? throw new ArgumentNullException(nameof(report));
        _navigation = navigation;

        FreedText = ByteSize.Humanize(report.BytesFreed);
        TimeText = FormatDuration(report.FinishedUtc - report.StartedUtc);
        SkippedCount = report.SkippedCount;
        QuarantinedCount = report.Items.Count(i => i.Outcome == CleanOutcome.Quarantined);
        DeletedCount = report.Items.Count(i => i.Outcome == CleanOutcome.Deleted);

        // Group freed space by top-level location for a simple per-area breakdown.
        foreach (var g in report.Items
                     .Where(i => i.Outcome is CleanOutcome.Deleted or CleanOutcome.Quarantined)
                     .GroupBy(i => i.Action.ToString())
                     .OrderByDescending(g => g.Sum(i => i.Size)))
        {
            Categories.Add(new ReportCategory(g.Key, ByteSize.Humanize(g.Sum(i => i.Size)), g.Sum(i => i.Size)));
        }
    }

    /// <summary>Total freed, formatted (the big figure).</summary>
    public string FreedText { get; }

    /// <summary>Duration text, e.g. "1 min 12 s".</summary>
    public string TimeText { get; }

    /// <summary>Whether the clean was cancelled (partial).</summary>
    public bool WasCancelled => _report.Cancelled;

    /// <summary>Deleted item count.</summary>
    public int DeletedCount { get; }

    /// <summary>Quarantined item count.</summary>
    public int QuarantinedCount { get; }

    /// <summary>Skipped item count.</summary>
    public int SkippedCount { get; }

    /// <summary>Per-area freed-space breakdown.</summary>
    public ObservableCollection<ReportCategory> Categories { get; } = [];

    /// <summary>Opens the IVX Clean logs folder.</summary>
    [RelayCommand]
    private void ViewLog()
    {
        try
        {
            string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IVXClean", "logs");
            System.IO.Directory.CreateDirectory(dir);
            Process.Start(new ProcessStartInfo { FileName = dir, UseShellExecute = true });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or System.IO.IOException)
        {
            // Ignore; opening the folder is a convenience.
        }
    }

    private static string FormatDuration(TimeSpan span)
        => span.TotalMinutes >= 1
            ? $"{(int)span.TotalMinutes} min {span.Seconds} s"
            : $"{span.Seconds} s";
}
