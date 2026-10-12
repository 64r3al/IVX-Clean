using System.Text.Json.Serialization;

namespace IvxClean.Core.Rules;

/// <summary>
/// One reviewed entry in the rule catalog. A rule is the <em>only</em> way a location becomes
/// eligible for cleaning: if no rule covers a path, it does not exist to the cleaner.
/// </summary>
public sealed class Rule
{
    /// <summary>Stable unique identifier, e.g. <c>browser.chromium.cache</c>.</summary>
    [JsonPropertyName("id")] public required string Id { get; init; }

    /// <summary>Human-readable name shown in Review.</summary>
    [JsonPropertyName("name")] public required string Name { get; init; }

    /// <summary>UI category grouping (Browsers, GPU &amp; shader caches, Windows Update, …).</summary>
    [JsonPropertyName("category")] public required string Category { get; init; }

    /// <summary>Cleaning level 1 (Quick) … 4 (Expert).</summary>
    [JsonPropertyName("level")] public required int Level { get; init; }

    /// <summary>Risk classification.</summary>
    [JsonPropertyName("risk")] public required RuleRisk Risk { get; init; }

    /// <summary>What the rule does with matched items.</summary>
    [JsonPropertyName("action")] public required RuleAction Action { get; init; }

    /// <summary>Detection conditions; the rule is skipped entirely when none match.</summary>
    [JsonPropertyName("detect")] public RuleDetect? Detect { get; init; }

    /// <summary>The locations the rule cleans.</summary>
    [JsonPropertyName("targets")]
    public IReadOnlyList<RuleTarget> Targets { get => field; init => field = value ?? []; } = [];

    /// <summary>Process executables that lock these items; if running, items are shown as skipped.</summary>
    [JsonPropertyName("requiresClosed")]
    public IReadOnlyList<string> RequiresClosed { get => field; init => field = value ?? []; } = [];

    /// <summary>Optional services to stop/restore (Windows Update rule only).</summary>
    [JsonPropertyName("services")]
    public IReadOnlyList<string> Services { get => field; init => field = value ?? []; } = [];

    /// <summary>For <see cref="RuleAction.Command"/>: the built-in operation name.</summary>
    [JsonPropertyName("command")] public string? Command { get; init; }

    /// <summary>Plain-language explanation of what is and is not touched (shown via the ⓘ button).</summary>
    [JsonPropertyName("why")] public required string Why { get; init; }

    /// <summary>Evidence/rationale that each target is a self-rebuilding cache or log.</summary>
    [JsonPropertyName("sources")]
    public IReadOnlyList<string> Sources { get => field; init => field = value ?? []; } = [];
}

/// <summary>Detection conditions for a rule.</summary>
public sealed class RuleDetect
{
    /// <summary>The rule applies if any of these tokenized paths exists.</summary>
    [JsonPropertyName("anyExists")]
    public IReadOnlyList<string> AnyExists { get => field; init => field = value ?? []; } = [];

    /// <summary>The rule applies only if a process from this list has ever been installed/detected (optional).</summary>
    [JsonPropertyName("anyProcessKnown")]
    public IReadOnlyList<string> AnyProcessKnown { get => field; init => field = value ?? []; } = [];
}

/// <summary>One cleanable location within a rule.</summary>
public sealed class RuleTarget
{
    /// <summary>The tokenized base folder, e.g. <c>{LocalAppData}\Google\Chrome\User Data</c>.</summary>
    [JsonPropertyName("base")] public required string Base { get; init; }

    /// <summary>
    /// When set, the base is treated as a parent of profile folders: each immediate subfolder that
    /// contains the named file is itself used as a base for <see cref="SubPaths"/>.
    /// </summary>
    [JsonPropertyName("profileDirs")] public ProfileDirSpec? ProfileDirs { get; init; }

    /// <summary>Relative subfolders under the (possibly per-profile) base that are cleaned.</summary>
    [JsonPropertyName("subPaths")]
    public IReadOnlyList<string> SubPaths { get => field; init => field = value ?? []; } = [];

    /// <summary>
    /// When set, only files whose name matches one of these globs (e.g. <c>thumbcache_*.db</c>) are
    /// eligible; directories are then never removed. When empty, all contents are eligible.
    /// </summary>
    [JsonPropertyName("fileGlobs")]
    public IReadOnlyList<string> FileGlobs { get => field; init => field = value ?? []; } = [];

    /// <summary>Minimum age in hours a file must have to be eligible. 0 disables the age gate.</summary>
    [JsonPropertyName("minAgeHours")] public int MinAgeHours { get; init; }

    /// <summary>Whether the resolved base may itself be deleted when empty.</summary>
    [JsonPropertyName("deleteBaseIfEmpty")] public bool DeleteBaseIfEmpty { get; init; }
}

/// <summary>Describes how to recognise per-profile subfolders under a target base.</summary>
public sealed class ProfileDirSpec
{
    /// <summary>A file that must exist in a subfolder for it to be treated as a profile folder.</summary>
    [JsonPropertyName("requireFile")] public string? RequireFile { get; init; }
}
