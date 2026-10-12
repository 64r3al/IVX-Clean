namespace IvxClean.Core.Safety;

/// <summary>
/// Why the <see cref="SafetyGuard"/> refused a path. Every denial carries one of these so the
/// Review and Report screens can explain honestly why an item was protected or skipped.
/// </summary>
public enum DenyReason
{
    /// <summary>No denial — the path is allowed.</summary>
    None = 0,

    /// <summary>The path could not be canonicalized or contains invalid characters.</summary>
    NotCanonicalizable,

    /// <summary>The path is a UNC/network path, device path, or alternate data stream.</summary>
    DangerousPathForm,

    /// <summary>The item, or an intervening directory, is a reparse point (junction/symlink/mount).</summary>
    ReparsePoint,

    /// <summary>The item is not strictly inside the rule's resolved base folder.</summary>
    OutsideBase,

    /// <summary>The item is the rule's base folder itself, which is not deletable.</summary>
    BaseFolder,

    /// <summary>The item is, contains, or is contained by a protected system location.</summary>
    ProtectedSystemLocation,

    /// <summary>The item is inside a protected user folder (Documents, Downloads, OneDrive, …).</summary>
    ProtectedUserFolder,

    /// <summary>The item is inside a folder the user marked as protected in Settings.</summary>
    UserProtectedFolder,

    /// <summary>The item matches a protected file/folder pattern (autosave, recovery, projects, …).</summary>
    ProtectedPattern,

    /// <summary>The item is a cloud placeholder / offline file and must not be touched.</summary>
    CloudPlaceholder,

    /// <summary>The item was modified more recently than the rule's minimum age.</summary>
    TooRecent,

    /// <summary>The item belongs to IVX Clean itself (quarantine, logs, settings, executable).</summary>
    OwnAppData,

    /// <summary>The item vanished or could not be resolved between check and use.</summary>
    Unresolvable,
}
