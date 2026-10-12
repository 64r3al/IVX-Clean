namespace IvxClean.Core.Scanning;

/// <summary>
/// The immutable result of a scan: every discovered item with its decision. The UI groups by rule
/// and category; totals are derived here so every surface agrees on the numbers.
/// </summary>
public sealed class CleanPlan
{
    /// <summary>Creates a plan for the given level from a set of items.</summary>
    public CleanPlan(int level, bool includedExpert, IReadOnlyList<PlanItem> items)
    {
        Level = level;
        IncludedExpert = includedExpert;
        Items = items ?? throw new ArgumentNullException(nameof(items));
    }

    /// <summary>The level the scan ran at.</summary>
    public int Level { get; }

    /// <summary>Whether Expert rules were included.</summary>
    public bool IncludedExpert { get; }

    /// <summary>Every item discovered, in discovery order.</summary>
    public IReadOnlyList<PlanItem> Items { get; }

    /// <summary>Items eligible to clean.</summary>
    public IEnumerable<PlanItem> Includable => Items.Where(i => i.Decision == PlanDecision.Include);

    /// <summary>Total bytes of includable items.</summary>
    public long IncludableBytes => Includable.Sum(i => i.Size);

    /// <summary>Total bytes of includable items that would be deleted directly.</summary>
    public long DeleteBytes => Includable.Where(i => i.Action == Rules.RuleAction.Delete).Sum(i => i.Size);

    /// <summary>Total bytes of includable items that would be quarantined.</summary>
    public long QuarantineBytes => Includable.Where(i => i.Action == Rules.RuleAction.Quarantine).Sum(i => i.Size);

    /// <summary>Count of protected items.</summary>
    public int ProtectedCount => Items.Count(i => i.Decision == PlanDecision.Protected);

    /// <summary>Count of skipped items.</summary>
    public int SkippedCount => Items.Count(i => i.Decision == PlanDecision.Skipped);
}
