namespace IvxClean.Core.Scanning;

/// <summary>Runs a read-only scan and returns a <see cref="CleanPlan"/>. Deletes nothing.</summary>
public interface IScanner
{
    /// <summary>
    /// Scans every rule up to <paramref name="level"/> (optionally including Expert rules),
    /// reporting progress and honouring cancellation. Scanning is strictly read-only.
    /// </summary>
    Task<CleanPlan> ScanAsync(int level, bool includeExpert, IProgress<ScanProgress>? progress, CancellationToken cancellationToken);
}
