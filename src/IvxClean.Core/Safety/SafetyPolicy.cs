using IvxClean.Core.Interop;

namespace IvxClean.Core.Safety;

/// <summary>
/// The configurable inputs to the <see cref="SafetyGuard"/>: protected file patterns, the
/// resolved protected user folders (Known Folders + OneDrive), and any folders the user marked
/// protected in Settings. The fixed system protected roots are derived from <see cref="Paths.IPathRoots"/>
/// by the guard itself.
/// </summary>
public sealed class SafetyPolicy
{
    /// <summary>Creates a policy from its parts.</summary>
    public SafetyPolicy(
        ProtectedPatterns patterns,
        IEnumerable<string> protectedUserFolders,
        IEnumerable<string> userDefinedProtectedFolders)
    {
        ArgumentNullException.ThrowIfNull(patterns);
        Patterns = patterns;
        ProtectedUserFolders = Normalize(protectedUserFolders);
        UserDefinedProtectedFolders = Normalize(userDefinedProtectedFolders);
    }

    /// <summary>The protected file/folder patterns.</summary>
    public ProtectedPatterns Patterns { get; }

    /// <summary>Resolved protected user folders (Documents, Downloads, OneDrive, …). No-touch subtrees.</summary>
    public IReadOnlyList<string> ProtectedUserFolders { get; }

    /// <summary>Folders the user added to the protected list in Settings. No-touch subtrees.</summary>
    public IReadOnlyList<string> UserDefinedProtectedFolders { get; }

    /// <summary>
    /// Builds the production policy from the current machine's Known Folders and the user's
    /// configured protected folders.
    /// </summary>
    public static SafetyPolicy CreateProduction(IEnumerable<string>? userDefinedProtectedFolders = null)
        => new(
            ProtectedPatterns.LoadEmbedded(),
            KnownFolders.GetProtectedUserFolders(),
            userDefinedProtectedFolders ?? []);

    private static IReadOnlyList<string> Normalize(IEnumerable<string> paths)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string p in paths)
        {
            if (string.IsNullOrWhiteSpace(p))
            {
                continue;
            }

            try
            {
                set.Add(Path.TrimEndingDirectorySeparator(Path.GetFullPath(p)));
            }
            catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
            {
                // A malformed configured path is ignored rather than weakening the guard.
            }
        }

        return [.. set];
    }
}
