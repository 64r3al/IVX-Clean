using System.Text.Json;
using IvxClean.Core.Abstractions;
using IvxClean.Core.Paths;

namespace IvxClean.Core.Quarantine;

/// <summary>File-system quarantine store. Moves items to a hidden per-volume folder and keeps a
/// central manifest per run so items can be restored or purged.</summary>
public sealed class QuarantineStore : IQuarantineStore
{
    private const string FolderName = "IVXClean.Quarantine";
    private const string ManifestName = "manifest.json";

    private readonly IPathRoots _roots;
    private readonly IClock _clock;
    private readonly int _retentionDays;
    private readonly bool _hardenAcls;
    private readonly Dictionary<string, QuarantineManifest> _active = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Creates the store. <paramref name="hardenAcls"/> restricts quarantine folders to
    /// Administrators/SYSTEM in production; tests pass <see langword="false"/>.</summary>
    public QuarantineStore(IPathRoots roots, IClock clock, int retentionDays = 7, bool hardenAcls = true)
    {
        _roots = roots ?? throw new ArgumentNullException(nameof(roots));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _retentionDays = Math.Clamp(retentionDays, 1, 30);
        _hardenAcls = hardenAcls;
    }

    private string CentralRoot => Path.Combine(_roots.ProgramData, "IVXClean", "Quarantine");

    // The system volume is wherever ProgramData lives; items on it quarantine centrally, items on
    // other volumes quarantine on their own volume (so every move is same-volume and fast).
    private string SystemVolume => Path.GetPathRoot(_roots.ProgramData) ?? _roots.SystemDrive;

    /// <inheritdoc />
    public string BeginRun(string description)
    {
        string runId = $"{_clock.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..24];
        var manifest = new QuarantineManifest { RunId = runId, CreatedUtc = _clock.UtcNow, Description = description ?? string.Empty };
        _active[runId] = manifest;
        CreateHidden(Path.Combine(CentralRoot, runId));
        return runId;
    }

    /// <inheritdoc />
    public string? TryQuarantine(string runId, string originalPath, long size, DateTime lastWriteUtc)
    {
        QuarantineManifest manifest = _active.TryGetValue(runId, out QuarantineManifest? m) ? m : LoadManifest(runId)
            ?? throw new InvalidOperationException($"Unknown quarantine run '{runId}'.");

        try
        {
            string volume = Path.GetPathRoot(originalPath) ?? SystemVolume;
            string storedDir = PathEquals(volume, SystemVolume)
                ? Path.Combine(CentralRoot, runId, "files")
                : Path.Combine(volume, FolderName, runId);
            CreateHidden(storedDir);

            string storedPath = UniqueName(Path.Combine(storedDir, $"{manifest.Entries.Count:D5}_{Path.GetFileName(originalPath)}"));

            // Clear read-only on the source so the move cannot be blocked by that attribute alone.
            TryClearReadOnly(originalPath);
            File.Move(originalPath, storedPath);

            manifest.Entries.Add(new QuarantineEntry
            {
                StoredPath = storedPath,
                OriginalPath = originalPath,
                Size = size,
                LastWriteUtc = lastWriteUtc,
            });
            return storedPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public void CommitRun(string runId)
    {
        if (!_active.TryGetValue(runId, out QuarantineManifest? manifest))
        {
            return;
        }

        string dir = Path.Combine(CentralRoot, runId);
        CreateHidden(dir);
        File.WriteAllText(Path.Combine(dir, ManifestName), JsonSerializer.Serialize(manifest, QuarantineJsonContext.Default.QuarantineManifest));
    }

    /// <inheritdoc />
    public IReadOnlyList<QuarantineRun> ListRuns()
    {
        var runs = new List<QuarantineRun>();
        if (!Directory.Exists(CentralRoot))
        {
            return runs;
        }

        foreach (string dir in Directory.EnumerateDirectories(CentralRoot))
        {
            QuarantineManifest? manifest = LoadManifestFromDir(dir);
            if (manifest is null)
            {
                continue;
            }

            int daysLeft = Math.Max(0, _retentionDays - (int)(_clock.UtcNow - manifest.CreatedUtc).TotalDays);
            runs.Add(new QuarantineRun(manifest.RunId, manifest.CreatedUtc, manifest.Description, manifest.Entries.Count, manifest.Entries.Sum(e => e.Size), daysLeft));
        }

        return [.. runs.OrderByDescending(r => r.CreatedUtc)];
    }

    /// <inheritdoc />
    public RestoreResult Restore(string runId, Func<string, RestoreConflictResolution> onConflict)
    {
        ArgumentNullException.ThrowIfNull(onConflict);
        QuarantineManifest? manifest = LoadManifest(runId);
        if (manifest is null)
        {
            return new RestoreResult(0, 0, 0);
        }

        int restored = 0, skipped = 0, failed = 0;
        foreach (QuarantineEntry entry in manifest.Entries)
        {
            try
            {
                if (!File.Exists(entry.StoredPath))
                {
                    failed++;
                    continue;
                }

                string destination = entry.OriginalPath;
                if (File.Exists(destination) || Directory.Exists(destination))
                {
                    if (onConflict(entry.OriginalPath) == RestoreConflictResolution.Skip)
                    {
                        skipped++;
                        continue;
                    }

                    destination = UniqueName(destination);
                }

                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                File.Move(entry.StoredPath, destination);
                restored++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                failed++;
            }
        }

        if (failed == 0 && skipped == 0)
        {
            DeleteRun(runId);
        }

        return new RestoreResult(restored, skipped, failed);
    }

    /// <inheritdoc />
    public void DeleteRun(string runId)
    {
        string centralRunDir = Path.Combine(CentralRoot, runId);
        QuarantineManifest? manifest = LoadManifest(runId);
        if (manifest is not null)
        {
            // Delete per-volume run folders (on drives other than the system drive).
            foreach (string storedDir in manifest.Entries
                         .Select(e => Path.GetDirectoryName(e.StoredPath)!)
                         .Where(d => !IsInside(d, centralRunDir) && !PathEquals(d, centralRunDir))
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                TryDeleteDirectory(storedDir);
            }
        }

        _active.Remove(runId);
        TryDeleteDirectory(centralRunDir);
    }

    /// <inheritdoc />
    public int PurgeExpired(int days)
    {
        int removed = 0;
        foreach (QuarantineRun run in ListRuns())
        {
            if ((_clock.UtcNow - run.CreatedUtc).TotalDays >= days)
            {
                DeleteRun(run.RunId);
                removed++;
            }
        }

        return removed;
    }

    private QuarantineManifest? LoadManifest(string runId)
        => _active.TryGetValue(runId, out QuarantineManifest? m) ? m : LoadManifestFromDir(Path.Combine(CentralRoot, runId));

    private static QuarantineManifest? LoadManifestFromDir(string dir)
    {
        string path = Path.Combine(dir, ManifestName);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            using FileStream stream = File.OpenRead(path);
            return JsonSerializer.Deserialize(stream, QuarantineJsonContext.Default.QuarantineManifest);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    private void CreateHidden(string dir)
    {
        var info = new DirectoryInfo(dir);
        if (!info.Exists)
        {
            info.Create();
            try
            {
                info.Attributes |= FileAttributes.Hidden;
            }
            catch (IOException)
            {
                // Attribute best-effort.
            }
        }

        if (_hardenAcls)
        {
            TryHardenAcls(info);
        }
    }

    private static void TryHardenAcls(DirectoryInfo info)
    {
        try
        {
            var security = new System.Security.AccessControl.DirectorySecurity();
            security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.BuiltinAdministratorsSid, null),
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                System.Security.AccessControl.PropagationFlags.None,
                System.Security.AccessControl.AccessControlType.Allow));
            security.AddAccessRule(new System.Security.AccessControl.FileSystemAccessRule(
                new System.Security.Principal.SecurityIdentifier(System.Security.Principal.WellKnownSidType.LocalSystemSid, null),
                System.Security.AccessControl.FileSystemRights.FullControl,
                System.Security.AccessControl.InheritanceFlags.ContainerInherit | System.Security.AccessControl.InheritanceFlags.ObjectInherit,
                System.Security.AccessControl.PropagationFlags.None,
                System.Security.AccessControl.AccessControlType.Allow));
            info.SetAccessControl(security);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or System.Security.SecurityException or IOException or PlatformNotSupportedException)
        {
            // Hardening is best-effort; failure must not break quarantine.
        }
    }

    private static void TryClearReadOnly(string path)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(path);
            if ((attributes & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(path, attributes & ~FileAttributes.ReadOnly);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Ignore; the move will report any real problem.
        }
    }

    private static void TryDeleteDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort.
        }
    }

    private static string UniqueName(string path)
    {
        if (!File.Exists(path) && !Directory.Exists(path))
        {
            return path;
        }

        string dir = Path.GetDirectoryName(path)!;
        string stem = Path.GetFileNameWithoutExtension(path);
        string ext = Path.GetExtension(path);
        for (int n = 1; n < int.MaxValue; n++)
        {
            string candidate = Path.Combine(dir, $"{stem} ({n}){ext}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("Could not find a unique name.");
    }

    private static bool PathEquals(string a, string b)
        => string.Equals(Path.TrimEndingDirectorySeparator(a), Path.TrimEndingDirectorySeparator(b), StringComparison.OrdinalIgnoreCase);

    private static bool IsInside(string child, string parent)
    {
        string p = Path.TrimEndingDirectorySeparator(parent);
        return child.Length > p.Length
            && child.StartsWith(p, StringComparison.OrdinalIgnoreCase)
            && (child[p.Length] == '\\' || child[p.Length] == '/');
    }
}

