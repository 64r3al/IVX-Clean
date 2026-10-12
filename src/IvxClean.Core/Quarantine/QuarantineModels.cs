using System.Text.Json.Serialization;

namespace IvxClean.Core.Quarantine;

/// <summary>One quarantined item, mapping its stored location back to where it came from.</summary>
public sealed class QuarantineEntry
{
    /// <summary>The absolute path where the item is now stored (on the source volume).</summary>
    [JsonPropertyName("storedPath")] public required string StoredPath { get; init; }

    /// <summary>The absolute original path to restore the item to.</summary>
    [JsonPropertyName("originalPath")] public required string OriginalPath { get; init; }

    /// <summary>The item's size in bytes.</summary>
    [JsonPropertyName("size")] public required long Size { get; init; }

    /// <summary>The item's original last-write time (UTC).</summary>
    [JsonPropertyName("lastWriteUtc")] public required DateTime LastWriteUtc { get; init; }
}

/// <summary>The manifest for one quarantine run.</summary>
public sealed class QuarantineManifest
{
    /// <summary>The run identifier.</summary>
    [JsonPropertyName("runId")] public required string RunId { get; init; }

    /// <summary>When the run was created (UTC).</summary>
    [JsonPropertyName("createdUtc")] public required DateTime CreatedUtc { get; init; }

    /// <summary>A short description of the run (e.g. "Standard clean").</summary>
    [JsonPropertyName("description")] public string Description { get; init; } = string.Empty;

    /// <summary>The quarantined items.</summary>
    [JsonPropertyName("entries")]
    public IList<QuarantineEntry> Entries { get => field; init => field = value ?? []; } = [];
}

/// <summary>A summary of a quarantine run for the Quarantine screen.</summary>
public sealed record QuarantineRun(string RunId, DateTime CreatedUtc, string Description, int ItemCount, long TotalBytes, int DaysLeft);

/// <summary>How to resolve a restore conflict when the original path is occupied.</summary>
public enum RestoreConflictResolution
{
    /// <summary>Skip restoring this item.</summary>
    Skip,

    /// <summary>Restore under a new, non-colliding name next to the original.</summary>
    KeepBoth,
}

/// <summary>The result of restoring a run.</summary>
public sealed record RestoreResult(int Restored, int Skipped, int Failed);

/// <summary>Source-generated JSON for the quarantine manifest.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(QuarantineManifest))]
public sealed partial class QuarantineJsonContext : JsonSerializerContext;
