namespace IvxClean.Core.Paths;

/// <summary>
/// Resolves every system location IVX Clean may reference. Rules and the cleaner must go
/// through this abstraction and never call <see cref="Environment.GetFolderPath(Environment.SpecialFolder)"/> or read
/// environment variables directly, so that tests can redirect all roots into a sandbox.
/// </summary>
/// <remarks>
/// Single-value roots resolve to one absolute path. The "each" roots enumerate zero or more
/// paths (one per user profile / fixed drive). <see cref="Expand"/> turns a tokenized template
/// such as <c>"{LocalAppData}\\Google\\Chrome"</c> into one or more concrete absolute paths.
/// </remarks>
public interface IPathRoots
{
    /// <summary>The current user's temporary folder (<c>%TEMP%</c>).</summary>
    string Temp { get; }

    /// <summary>The current user's local application data (<c>%LOCALAPPDATA%</c>).</summary>
    string LocalAppData { get; }

    /// <summary>The current user's roaming application data (<c>%APPDATA%</c>).</summary>
    string RoamingAppData { get; }

    /// <summary>The current user's LocalLow application data.</summary>
    string LocalLowAppData { get; }

    /// <summary>The machine-wide program data folder (<c>%PROGRAMDATA%</c>).</summary>
    string ProgramData { get; }

    /// <summary>The Windows directory (<c>%SYSTEMROOT%</c>).</summary>
    string Windows { get; }

    /// <summary>The system drive root, including the trailing separator (e.g. <c>C:\</c>).</summary>
    string SystemDrive { get; }

    /// <summary>The current user's profile root (<c>%USERPROFILE%</c>).</summary>
    string UserProfile { get; }

    /// <summary>IVX Clean's own per-user data folder (settings, logs). Always protected.</summary>
    string AppDataRoot { get; }

    /// <summary>The running executable's directory. Always protected.</summary>
    string AppInstallDirectory { get; }

    /// <summary>Every real user profile root (excludes Default, Public, system and service profiles).</summary>
    IReadOnlyList<string> EachUserProfile { get; }

    /// <summary>Every fixed, ready drive root (e.g. <c>C:\</c>, <c>D:\</c>).</summary>
    IReadOnlyList<string> EachFixedDrive { get; }

    /// <summary>
    /// Expands a tokenized template into one or more absolute paths. Unknown tokens cause an
    /// <see cref="InvalidOperationException"/>; a template with no token resolves to itself.
    /// "Each" tokens (<c>{EachUserProfile}</c>, <c>{EachFixedDrive}</c>) may yield several paths
    /// and may only appear as the leading segment.
    /// </summary>
    IReadOnlyList<string> Expand(string tokenizedPath);
}
