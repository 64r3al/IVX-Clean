using IvxClean.Core.Scanning;

namespace IvxClean.Core.Cleaning;

/// <summary>Executes a confirmed set of plan items, re-validating each immediately before acting.</summary>
public interface ICleaner
{
    /// <summary>
    /// Deletes or quarantines the given items (which must be the user-confirmed, includable file
    /// items), then removes directories that became empty. Cancellation stops after the current
    /// item and returns a partial report. Never throws for ordinary filesystem conditions.
    /// </summary>
    Task<CleanReport> CleanAsync(IReadOnlyList<PlanItem> items, CleanOptions options, IProgress<CleanProgress>? progress, CancellationToken cancellationToken);
}
