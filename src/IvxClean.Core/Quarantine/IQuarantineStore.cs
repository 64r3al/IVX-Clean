namespace IvxClean.Core.Quarantine;

/// <summary>
/// Stores quarantined items so Caution/Expert cleans can be undone. Items are moved (not copied)
/// to a hidden quarantine folder on their own volume; a manifest maps each back to its origin.
/// </summary>
public interface IQuarantineStore
{
    /// <summary>Starts a run with the given description and returns its run id.</summary>
    string BeginRun(string description);

    /// <summary>
    /// Moves <paramref name="originalPath"/> into the run's quarantine. Returns the stored path,
    /// or <see langword="null"/> if the move failed (the caller then skips the item).
    /// </summary>
    string? TryQuarantine(string runId, string originalPath, long size, DateTime lastWriteUtc);

    /// <summary>Persists the run's manifest. Call once after a run's items are quarantined.</summary>
    void CommitRun(string runId);

    /// <summary>Lists all quarantine runs, newest first.</summary>
    IReadOnlyList<QuarantineRun> ListRuns();

    /// <summary>Restores every item in a run to its original location.</summary>
    RestoreResult Restore(string runId, Func<string, RestoreConflictResolution> onConflict);

    /// <summary>Permanently deletes a run's quarantined items.</summary>
    void DeleteRun(string runId);

    /// <summary>Deletes runs older than <paramref name="days"/> days. Returns the number removed.</summary>
    int PurgeExpired(int days);
}
