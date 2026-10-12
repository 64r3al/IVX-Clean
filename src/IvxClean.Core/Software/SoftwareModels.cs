namespace IvxClean.Core.Software;

/// <summary>Where an installed-software entry came from.</summary>
public enum SoftwareSource
{
    /// <summary>A classic (MSI/EXE) program from the uninstall registry keys.</summary>
    Registry,

    /// <summary>A Store / AppX package for the current user.</summary>
    AppX,
}

/// <summary>How IVX Clean classifies an installed program.</summary>
public enum SoftwareClassification
{
    /// <summary>Hidden entirely; can never be uninstalled from the app (drivers, runtimes, anti-cheat, …).</summary>
    NeverList,

    /// <summary>Known junk/bloatware.</summary>
    KnownBloatware,

    /// <summary>Possibly unwanted (trialware, toolbars, "optimizers", very old runtimes).</summary>
    MaybeUnwanted,

    /// <summary>Everything else — kept.</summary>
    Keep,
}

/// <summary>The raw data read for one installed program, before classification.</summary>
public sealed record RawSoftwareEntry
{
    /// <summary>A stable identifier (registry key path or package full name).</summary>
    public required string Id { get; init; }

    /// <summary>The display name.</summary>
    public required string DisplayName { get; init; }

    /// <summary>The publisher, if known.</summary>
    public string Publisher { get; init; } = string.Empty;

    /// <summary>The version string, if known.</summary>
    public string Version { get; init; } = string.Empty;

    /// <summary>The install date (UTC), if known.</summary>
    public DateTime? InstallDateUtc { get; init; }

    /// <summary>The estimated size in bytes, if known.</summary>
    public long EstimatedSizeBytes { get; init; }

    /// <summary>The raw uninstall command (registry), if any.</summary>
    public string? UninstallString { get; init; }

    /// <summary>The quiet uninstall command (registry), if any.</summary>
    public string? QuietUninstallString { get; init; }

    /// <summary>The package full name (AppX), if any.</summary>
    public string? PackageFullName { get; init; }

    /// <summary>Where this entry came from.</summary>
    public required SoftwareSource Source { get; init; }
}

/// <summary>A classified installed program, ready for the Unwanted software screen.</summary>
public sealed record SoftwareEntry
{
    /// <summary>The underlying raw entry.</summary>
    public required RawSoftwareEntry Raw { get; init; }

    /// <summary>The classification.</summary>
    public required SoftwareClassification Classification { get; init; }

    /// <summary>An optional short hint shown to the user (e.g. "Expired trial").</summary>
    public string? Hint { get; init; }

    /// <summary>The display name (convenience).</summary>
    public string DisplayName => Raw.DisplayName;

    /// <summary>The publisher (convenience).</summary>
    public string Publisher => Raw.Publisher;
}
