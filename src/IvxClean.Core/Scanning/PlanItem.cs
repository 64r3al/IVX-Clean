using IvxClean.Core.Rules;

namespace IvxClean.Core.Scanning;

/// <summary>How a scanned item will be treated.</summary>
public enum PlanDecision
{
    /// <summary>Eligible to be cleaned (subject to the user's selection).</summary>
    Include,

    /// <summary>Permanently protected — never offered for cleaning.</summary>
    Protected,

    /// <summary>Temporarily skipped (in use, too recent, app open); may be included after a rescan.</summary>
    Skipped,
}

/// <summary>
/// One concrete item discovered by a scan, with the decision the SafetyGuard reached about it.
/// Immutable: a plan is a snapshot the user reviews and confirms.
/// </summary>
public sealed record PlanItem
{
    /// <summary>The rule that discovered the item.</summary>
    public required string RuleId { get; init; }

    /// <summary>The rule's category (for UI grouping).</summary>
    public required string Category { get; init; }

    /// <summary>The rule's risk classification.</summary>
    public required RuleRisk Risk { get; init; }

    /// <summary>The rule's action.</summary>
    public required RuleAction Action { get; init; }

    /// <summary>The absolute path of the item.</summary>
    public required string Path { get; init; }

    /// <summary>Whether the item is a directory.</summary>
    public required bool IsDirectory { get; init; }

    /// <summary>The item's size in bytes (0 for directories; files only).</summary>
    public required long Size { get; init; }

    /// <summary>The item's last-write time (UTC), captured at scan time for TOCTOU re-checks.</summary>
    public required DateTime LastWriteUtc { get; init; }

    /// <summary>The decision reached for this item.</summary>
    public required PlanDecision Decision { get; init; }

    /// <summary>A short, user-facing reason for the decision.</summary>
    public required string Reason { get; init; }
}
