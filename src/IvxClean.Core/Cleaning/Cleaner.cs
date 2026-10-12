using IvxClean.Core.Abstractions;
using IvxClean.Core.Interop;
using IvxClean.Core.IO;
using IvxClean.Core.Quarantine;
using IvxClean.Core.Rules;
using IvxClean.Core.Safety;
using IvxClean.Core.Scanning;

namespace IvxClean.Core.Cleaning;

/// <summary>
/// The cleaner. For every item: SafetyGuard re-check → still exists → not a reparse point →
/// last-write unchanged since the scan → delete or quarantine. Then removes directories that are
/// empty afterwards, bottom-up, never the rule's base (unless the rule opted in). Fails safe.
/// </summary>
public sealed class Cleaner : ICleaner
{
    private readonly ISafetyGuard _guard;
    private readonly IQuarantineStore _quarantine;
    private readonly IClock _clock;

    /// <summary>Creates the cleaner.</summary>
    public Cleaner(ISafetyGuard guard, IQuarantineStore quarantine, IClock clock)
    {
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
        _quarantine = quarantine ?? throw new ArgumentNullException(nameof(quarantine));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <inheritdoc />
    public Task<CleanReport> CleanAsync(IReadOnlyList<PlanItem> items, CleanOptions options, IProgress<CleanProgress>? progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(options);

        return Task.Run(() =>
        {
            string runId = $"{_clock.UtcNow:yyyyMMdd-HHmmss}";
            DateTime started = _clock.UtcNow;
            var results = new List<CleanResultItem>();
            string? quarantineRunId = null;
            bool cancelled = false;

            // Files only here; empty directories are removed afterwards.
            List<PlanItem> files = items.Where(i => !i.IsDirectory && i.Action != RuleAction.Command).ToList();
            int total = files.Count;
            int processed = 0;

            foreach (PlanItem item in files)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancelled = true;
                    break;
                }

                results.Add(ProcessFile(item, options, ref quarantineRunId));
                processed++;
                progress?.Report(new CleanProgress { Category = item.Category, CurrentPath = item.Path, ItemsProcessed = processed, ItemsTotal = total });
            }

            if (quarantineRunId is not null)
            {
                _quarantine.CommitRun(quarantineRunId);
            }

            if (!cancelled)
            {
                RemoveEmptyDirectories(files, cancellationToken, ref cancelled);
            }

            return new CleanReport(runId, started, _clock.UtcNow, cancelled, quarantineRunId, results);
        }); // Token is honoured cooperatively inside so cancellation yields a partial report, never a throw.
    }

    private CleanResultItem ProcessFile(PlanItem item, CleanOptions options, ref string? quarantineRunId)
    {
        var target = new SafetyTarget(item.BaseFolder, item.MinAgeHours, item.AllowBaseDeletion);

        // 1. Re-validate with the guard immediately before acting (defends against TOCTOU).
        SafetyDecision decision = _guard.Evaluate(item.Path, target);
        if (decision.IsDenied)
        {
            return new CleanResultItem(item.Path, item.Action, CleanOutcome.Skipped, $"Re-check denied: {decision.Explanation}", 0);
        }

        // 2. Still exists and is not a reparse point.
        if (!File.Exists(item.Path))
        {
            return new CleanResultItem(item.Path, item.Action, CleanOutcome.Skipped, "Vanished since scan.", 0);
        }

        if (PathResolver.IsReparsePoint(item.Path))
        {
            return new CleanResultItem(item.Path, item.Action, CleanOutcome.Skipped, "Became a reparse point since scan.", 0);
        }

        // 3. Unchanged since the scan.
        long size;
        try
        {
            var info = new FileInfo(item.Path);
            if (info.LastWriteTimeUtc != item.LastWriteUtc)
            {
                return new CleanResultItem(item.Path, item.Action, CleanOutcome.Skipped, "Changed since scan.", 0);
            }

            size = info.Length;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new CleanResultItem(item.Path, item.Action, CleanOutcome.Skipped, "In use / access denied.", 0);
        }

        // 4. Act.
        if (item.Action == RuleAction.Quarantine)
        {
            quarantineRunId ??= _quarantine.BeginRun(options.Description);
            string? stored = _quarantine.TryQuarantine(quarantineRunId, item.Path, size, item.LastWriteUtc);
            return stored is not null
                ? new CleanResultItem(item.Path, item.Action, CleanOutcome.Quarantined, "Moved to Quarantine.", size)
                : new CleanResultItem(item.Path, item.Action, CleanOutcome.Skipped, "In use / access denied.", 0);
        }

        try
        {
            ClearReadOnly(item.Path);
            File.Delete(item.Path);
            return new CleanResultItem(item.Path, item.Action, CleanOutcome.Deleted, "Deleted.", size);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new CleanResultItem(item.Path, item.Action, CleanOutcome.Skipped, "In use / access denied.", 0);
        }
    }

    private void RemoveEmptyDirectories(List<PlanItem> files, CancellationToken cancellationToken, ref bool cancelled)
    {
        // Map each distinct clean root to whether its base may be deleted when empty.
        var roots = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        foreach (PlanItem item in files)
        {
            roots[item.BaseFolder] = item.AllowBaseDeletion;
        }

        foreach ((string root, bool allowBase) in roots)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                cancelled = true;
                return;
            }

            if (!Directory.Exists(root))
            {
                continue;
            }

            // Deepest directories first so parents can become empty.
            List<string> dirs = SafeFileEnumerator.Enumerate(root, recursive: true, includeDirectories: true)
                .Where(e => e.IsDirectory)
                .Select(e => e.Path)
                .Append(root)
                .OrderByDescending(d => d.Count(c => c is '\\' or '/'))
                .ToList();

            foreach (string dir in dirs)
            {
                TryRemoveEmptyDirectory(dir, root, allowBase);
            }
        }
    }

    private void TryRemoveEmptyDirectory(string dir, string root, bool allowBase)
    {
        if (PathResolver.IsReparsePoint(dir))
        {
            return;
        }

        bool isRoot = string.Equals(
            Path.TrimEndingDirectorySeparator(dir),
            Path.TrimEndingDirectorySeparator(root),
            StringComparison.OrdinalIgnoreCase);
        if (isRoot && !allowBase)
        {
            return;
        }

        try
        {
            if (Directory.EnumerateFileSystemEntries(dir).Any())
            {
                return; // not empty
            }

            SafetyDecision decision = _guard.Evaluate(dir, new SafetyTarget(root, 0, allowBase));
            if (decision.IsAllowed)
            {
                Directory.Delete(dir, recursive: false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Leave a directory we cannot remove.
        }
    }

    private static void ClearReadOnly(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);
        if ((attributes & FileAttributes.ReadOnly) != 0)
        {
            File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
        }
    }
}
