using IvxClean.Core.Rules;

namespace IvxClean.Core.Cleaning;

/// <summary>What happened to one item during a clean.</summary>
public enum CleanOutcome
{
    /// <summary>Deleted directly.</summary>
    Deleted,

    /// <summary>Moved to Quarantine.</summary>
    Quarantined,

    /// <summary>Not acted upon (in use, changed since scan, re-check denied, vanished).</summary>
    Skipped,
}

/// <summary>The result for one item.</summary>
public sealed record CleanResultItem(string Path, RuleAction Action, CleanOutcome Outcome, string Reason, long Size);

/// <summary>Options controlling a clean.</summary>
public sealed record CleanOptions
{
    /// <summary>A short description recorded on any quarantine run (e.g. "Standard clean").</summary>
    public string Description { get; init; } = "Clean";
}

/// <summary>Progress reported as a clean runs.</summary>
public sealed record CleanProgress
{
    /// <summary>The category of the item just processed.</summary>
    public required string Category { get; init; }

    /// <summary>The path just processed.</summary>
    public required string CurrentPath { get; init; }

    /// <summary>Items processed so far.</summary>
    public required int ItemsProcessed { get; init; }

    /// <summary>Total items to process.</summary>
    public required int ItemsTotal { get; init; }
}

/// <summary>The outcome of a whole clean run.</summary>
public sealed class CleanReport
{
    /// <summary>Creates a report.</summary>
    public CleanReport(string runId, DateTime startedUtc, DateTime finishedUtc, bool cancelled, string? quarantineRunId, IReadOnlyList<CleanResultItem> items)
    {
        RunId = runId;
        StartedUtc = startedUtc;
        FinishedUtc = finishedUtc;
        Cancelled = cancelled;
        QuarantineRunId = quarantineRunId;
        Items = items ?? [];
    }

    /// <summary>The clean run identifier.</summary>
    public string RunId { get; }

    /// <summary>When the run started (UTC).</summary>
    public DateTime StartedUtc { get; }

    /// <summary>When the run finished (UTC).</summary>
    public DateTime FinishedUtc { get; }

    /// <summary>Whether the run was cancelled (then it is a partial report).</summary>
    public bool Cancelled { get; }

    /// <summary>The quarantine run id, if anything was quarantined.</summary>
    public string? QuarantineRunId { get; }

    /// <summary>Per-item results.</summary>
    public IReadOnlyList<CleanResultItem> Items { get; }

    /// <summary>Total bytes deleted.</summary>
    public long BytesDeleted => Items.Where(i => i.Outcome == CleanOutcome.Deleted).Sum(i => i.Size);

    /// <summary>Total bytes quarantined.</summary>
    public long BytesQuarantined => Items.Where(i => i.Outcome == CleanOutcome.Quarantined).Sum(i => i.Size);

    /// <summary>Total bytes freed (deleted + quarantined).</summary>
    public long BytesFreed => BytesDeleted + BytesQuarantined;

    /// <summary>Number of items skipped.</summary>
    public int SkippedCount => Items.Count(i => i.Outcome == CleanOutcome.Skipped);
}
