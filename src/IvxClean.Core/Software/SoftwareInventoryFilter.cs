namespace IvxClean.Core.Software;

/// <summary>Raw values read from one uninstall registry key.</summary>
public sealed record RegistryUninstallRecord
{
    /// <summary>The registry key identifier (path).</summary>
    public required string KeyId { get; init; }

    /// <summary>The DisplayName value.</summary>
    public string? DisplayName { get; init; }

    /// <summary>The Publisher value.</summary>
    public string? Publisher { get; init; }

    /// <summary>The DisplayVersion value.</summary>
    public string? Version { get; init; }

    /// <summary>The SystemComponent DWORD (1 hides the entry).</summary>
    public int? SystemComponent { get; init; }

    /// <summary>The ParentKeyName value (child components have one).</summary>
    public string? ParentKeyName { get; init; }

    /// <summary>The ReleaseType value (Update/Hotfix/Security Update are excluded).</summary>
    public string? ReleaseType { get; init; }

    /// <summary>The WindowsInstaller DWORD (1 means an MSI product).</summary>
    public int? WindowsInstaller { get; init; }

    /// <summary>The UninstallString value.</summary>
    public string? UninstallString { get; init; }

    /// <summary>The QuietUninstallString value.</summary>
    public string? QuietUninstallString { get; init; }

    /// <summary>The EstimatedSize value in KB (registry stores KB).</summary>
    public long EstimatedSizeKb { get; init; }

    /// <summary>The parsed InstallDate (UTC), if present.</summary>
    public DateTime? InstallDateUtc { get; init; }
}

/// <summary>Decides which registry uninstall entries are real, user-facing programs.</summary>
public static class SoftwareInventoryFilter
{
    private static readonly HashSet<string> ExcludedReleaseTypes =
        new(["Update", "Hotfix", "Security Update", "ServicePack"], StringComparer.OrdinalIgnoreCase);

    /// <summary>Returns true if the entry should appear in the inventory.</summary>
    public static bool ShouldInclude(RegistryUninstallRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);

        if (string.IsNullOrWhiteSpace(record.DisplayName))
        {
            return false;
        }

        if (record.SystemComponent == 1)
        {
            return false;
        }

        if (!string.IsNullOrEmpty(record.ParentKeyName))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(record.ReleaseType) && ExcludedReleaseTypes.Contains(record.ReleaseType))
        {
            return false;
        }

        // Must have some way to be uninstalled.
        bool canUninstall = !string.IsNullOrWhiteSpace(record.UninstallString)
            || !string.IsNullOrWhiteSpace(record.QuietUninstallString)
            || record.WindowsInstaller == 1;

        return canUninstall;
    }

    /// <summary>Maps an included registry record to a raw software entry.</summary>
    public static RawSoftwareEntry ToRawEntry(RegistryUninstallRecord record)
        => new()
        {
            Id = record.KeyId,
            DisplayName = record.DisplayName ?? string.Empty,
            Publisher = record.Publisher ?? string.Empty,
            Version = record.Version ?? string.Empty,
            InstallDateUtc = record.InstallDateUtc,
            EstimatedSizeBytes = record.EstimatedSizeKb * 1024,
            UninstallString = record.UninstallString,
            QuietUninstallString = record.QuietUninstallString,
            Source = SoftwareSource.Registry,
        };
}
