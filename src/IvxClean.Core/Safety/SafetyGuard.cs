using IvxClean.Core.Abstractions;
using IvxClean.Core.Interop;
using IvxClean.Core.Paths;

namespace IvxClean.Core.Safety;

/// <summary>
/// The defense-in-depth gate described in the IVX Clean prime directive. See <see cref="ISafetyGuard"/>.
/// </summary>
public sealed class SafetyGuard : ISafetyGuard
{
    // Cloud placeholder / offline attributes (values from winnt.h, also surfaced by FileAttributes).
    private const FileAttributes RecallOnDataAccess = (FileAttributes)0x400000;
    private const FileAttributes RecallOnOpen = (FileAttributes)0x40000;

    private static readonly string[] WindowsNoTouchSubfolders =
    [
        "System32", "SysWOW64", "WinSxS", "Boot", "Fonts", "Installer", "servicing", "assembly",
    ];

    private static readonly string[] DriveRootNoTouchFolders =
    [
        "System Volume Information", "$Recycle.Bin", "Windows.old", "Recovery", "EFI",
        "$Windows.~BT", "$Windows.~WS", "$GetCurrent",
    ];

    private static readonly string[] DriveRootNoTouchFiles =
    [
        "pagefile.sys", "hiberfil.sys", "swapfile.sys",
    ];

    private readonly IClock _clock;
    private readonly SafetyPolicy _policy;

    // Category A: no-touch subtrees — deny if candidate equals, is inside, or is an ancestor of one.
    private readonly string[] _noTouchTrees;

    // Category A: exact no-touch files.
    private readonly HashSet<string> _noTouchFiles;

    // Category B: container roots — deny only if candidate equals or is an ancestor; inside is allowed.
    private readonly string[] _containerRoots;

    /// <summary>Creates the guard for the given path roots, clock and policy.</summary>
    public SafetyGuard(IPathRoots roots, IClock clock, SafetyPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(roots);
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _policy = policy ?? throw new ArgumentNullException(nameof(policy));

        (_noTouchTrees, _noTouchFiles, _containerRoots) = BuildProtectedSets(roots, policy);
    }

    /// <inheritdoc />
    public SafetyDecision Evaluate(string candidatePath, ISafetyTarget target)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.IsNullOrWhiteSpace(candidatePath))
        {
            return SafetyDecision.Deny(DenyReason.NotCanonicalizable, "Empty path.");
        }

        // 1. Dangerous path forms, checked on the raw input first.
        if (HasDangerousForm(candidatePath))
        {
            return SafetyDecision.Deny(DenyReason.DangerousPathForm, "Network, device or alternate-data-stream path.");
        }

        // 2. Canonicalize.
        string lexical;
        try
        {
            lexical = Normalize(candidatePath);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
        {
            return SafetyDecision.Deny(DenyReason.NotCanonicalizable, "Path could not be canonicalized.");
        }

        if (HasDangerousForm(lexical))
        {
            return SafetyDecision.Deny(DenyReason.DangerousPathForm, "Network, device or alternate-data-stream path.");
        }

        // 3. Resolve the rule's base to its final path.
        string baseFinal;
        try
        {
            string baseLexical = Normalize(target.ResolvedBase);
            if (!PathResolver.TryGetFinalPath(baseLexical, out baseFinal) || baseFinal.Length == 0)
            {
                return SafetyDecision.Deny(DenyReason.Unresolvable, "Rule base folder could not be resolved.");
            }

            baseFinal = Normalize(baseFinal);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return SafetyDecision.Deny(DenyReason.Unresolvable, "Rule base folder is invalid.");
        }

        // Defense in depth: refuse a base that is itself a drive root or a broad container root
        // (e.g. a bare drive, Windows, ProgramData, a profile or an AppData root). Cleaning directly
        // inside those is never safe; a legitimate rule always targets a specific subfolder.
        if (IsTooBroadBase(baseFinal))
        {
            return SafetyDecision.Deny(DenyReason.ProtectedSystemLocation, "The rule's base folder is too broad to clean safely.");
        }

        // 4. The candidate itself must not be a reparse point.
        if (PathResolver.IsReparsePoint(lexical))
        {
            return SafetyDecision.Deny(DenyReason.ReparsePoint, "Item is a junction, symlink or mount point.");
        }

        // 5. No intervening directory between base and item may be a reparse point.
        if (HasReparsePointBetween(baseFinal, lexical))
        {
            return SafetyDecision.Deny(DenyReason.ReparsePoint, "A parent folder is a junction, symlink or mount point.");
        }

        // 6. Resolve the candidate's final path and require it is within the final base.
        if (!PathResolver.TryGetFinalPath(lexical, out string candidateFinal) || candidateFinal.Length == 0)
        {
            return SafetyDecision.Deny(DenyReason.Unresolvable, "Item vanished or could not be resolved.");
        }

        candidateFinal = Normalize(candidateFinal);

        bool isBaseItself = PathEquals(candidateFinal, baseFinal);
        if (isBaseItself)
        {
            if (!target.AllowBaseDeletion)
            {
                return SafetyDecision.Deny(DenyReason.BaseFolder, "The rule's base folder is never deleted.");
            }
        }
        else if (!IsInside(candidateFinal, baseFinal) || !IsInside(lexical, Normalize(target.ResolvedBase)))
        {
            return SafetyDecision.Deny(DenyReason.OutsideBase, "Item resolves outside the rule's base folder.");
        }

        // 7. Protected system/user/own locations (checked on the true resolved path).
        SafetyDecision protectedDecision = EvaluateProtectedLocations(candidateFinal);
        if (protectedDecision.IsDenied)
        {
            return protectedDecision;
        }

        // 8. Protected file/folder patterns (autosave, recovery, project files, …).
        if (_policy.Patterns.MatchesProtectedFolder(candidateFinal))
        {
            return SafetyDecision.Deny(DenyReason.ProtectedPattern, "Inside a protected autosave/recovery/backup folder.");
        }

        if (_policy.Patterns.MatchesFileName(candidateFinal))
        {
            return SafetyDecision.Deny(DenyReason.ProtectedPattern, "Protected recovery, autosave or project file.");
        }

        // 9. Cloud placeholder / offline files are never touched.
        FileAttributes attributes;
        try
        {
            attributes = File.GetAttributes(lexical);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return SafetyDecision.Deny(DenyReason.Unresolvable, "Item could not be read.");
        }

        if ((attributes & (RecallOnDataAccess | RecallOnOpen | FileAttributes.Offline)) != 0)
        {
            return SafetyDecision.Deny(DenyReason.CloudPlaceholder, "Cloud placeholder / offline file.");
        }

        // 10. Age gate (files only).
        bool isDirectory = (attributes & FileAttributes.Directory) != 0;
        if (!isDirectory && target.MinAgeHours > 0)
        {
            try
            {
                DateTime lastWriteUtc = File.GetLastWriteTimeUtc(lexical);
                if (_clock.UtcNow - lastWriteUtc < TimeSpan.FromHours(target.MinAgeHours))
                {
                    return SafetyDecision.Deny(DenyReason.TooRecent, "Modified too recently.");
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return SafetyDecision.Deny(DenyReason.Unresolvable, "Item age could not be read.");
            }
        }

        return SafetyDecision.Allowed;
    }

    private SafetyDecision EvaluateProtectedLocations(string candidateFinal)
    {
        if (_noTouchFiles.Contains(candidateFinal))
        {
            return SafetyDecision.Deny(DenyReason.ProtectedSystemLocation, "Protected system file.");
        }

        foreach (string tree in _noTouchTrees)
        {
            if (PathEquals(candidateFinal, tree) || IsInside(candidateFinal, tree) || IsInside(tree, candidateFinal))
            {
                return ClassifyNoTouch(tree, candidateFinal);
            }
        }

        foreach (string container in _containerRoots)
        {
            // Deny only if the candidate is the container itself or an ancestor of it.
            if (PathEquals(candidateFinal, container) || IsInside(container, candidateFinal))
            {
                return SafetyDecision.Deny(DenyReason.ProtectedSystemLocation, "A protected root folder.");
            }
        }

        return SafetyDecision.Allowed;
    }

    private SafetyDecision ClassifyNoTouch(string tree, string candidate)
    {
        if (IsInAny(tree, _policy.ProtectedUserFolders) || IsInAny(candidate, _policy.ProtectedUserFolders))
        {
            return SafetyDecision.Deny(DenyReason.ProtectedUserFolder, "Inside a protected personal folder.");
        }

        if (IsInAny(tree, _policy.UserDefinedProtectedFolders) || IsInAny(candidate, _policy.UserDefinedProtectedFolders))
        {
            return SafetyDecision.Deny(DenyReason.UserProtectedFolder, "Inside a folder you marked protected.");
        }

        return SafetyDecision.Deny(DenyReason.ProtectedSystemLocation, "A protected system location.");
    }

    private static bool IsInAny(string path, IReadOnlyList<string> roots)
    {
        foreach (string r in roots)
        {
            if (PathEquals(path, r) || IsInside(path, r))
            {
                return true;
            }
        }

        return false;
    }

    private static (string[] NoTouchTrees, HashSet<string> NoTouchFiles, string[] ContainerRoots) BuildProtectedSets(
        IPathRoots roots, SafetyPolicy policy)
    {
        var noTouch = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var containers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddTree(string p)
        {
            if (!string.IsNullOrWhiteSpace(p))
            {
                noTouch.Add(Normalize(p));
            }
        }

        void AddContainer(string p)
        {
            if (!string.IsNullOrWhiteSpace(p))
            {
                containers.Add(Normalize(p));
            }
        }

        string windows = roots.Windows;
        foreach (string sub in WindowsNoTouchSubfolders)
        {
            AddTree(Path.Combine(windows, sub));
        }

        // Program Files from both the system drive and the environment (covers relocated installs).
        AddTree(Path.Combine(roots.SystemDrive, "Program Files"));
        AddTree(Path.Combine(roots.SystemDrive, "Program Files (x86)"));
        AddProgramFilesFromEnvironment(AddTree);

        // IVX Clean's own folders.
        AddTree(roots.AppDataRoot);
        AddTree(roots.AppInstallDirectory);
        AddTree(Path.Combine(roots.ProgramData, "IVXClean"));

        // Per-drive protected trees, files, and quarantine locations.
        foreach (string drive in roots.EachFixedDrive)
        {
            AddContainer(drive);
            foreach (string folder in DriveRootNoTouchFolders)
            {
                AddTree(Path.Combine(drive, folder));
            }

            foreach (string file in DriveRootNoTouchFiles)
            {
                files.Add(Normalize(Path.Combine(drive, file)));
            }

            AddTree(Path.Combine(drive, "IVXClean.Quarantine"));
            AddContainer(Path.Combine(drive, "Users"));
        }

        // Windows, ProgramData and the system drive are container roots (deletable neither as a
        // whole nor as a parent, but traversable).
        AddContainer(windows);
        AddContainer(roots.ProgramData);
        AddContainer(roots.SystemDrive);

        // Each user profile and its AppData roots are container roots.
        foreach (string profile in roots.EachUserProfile)
        {
            AddContainer(profile);
            AddContainer(Path.Combine(profile, "AppData"));
            AddContainer(Path.Combine(profile, "AppData", "Local"));
            AddContainer(Path.Combine(profile, "AppData", "Roaming"));
            AddContainer(Path.Combine(profile, "AppData", "LocalLow"));
        }

        AddContainer(roots.UserProfile);
        AddContainer(roots.LocalAppData);
        AddContainer(roots.RoamingAppData);
        AddContainer(roots.LocalLowAppData);

        // Protected user folders and user-defined folders are no-touch subtrees.
        foreach (string p in policy.ProtectedUserFolders)
        {
            AddTree(p);
        }

        foreach (string p in policy.UserDefinedProtectedFolders)
        {
            AddTree(p);
        }

        return ([.. noTouch], files, [.. containers]);
    }

    private static void AddProgramFilesFromEnvironment(Action<string> add)
    {
        string pf = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        string pfx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        if (pf.Length > 0)
        {
            add(pf);
        }

        if (pfx86.Length > 0)
        {
            add(pfx86);
        }
    }

    /// <summary>Returns true if the item at <paramref name="start"/> has a reparse-point ancestor up to (but excluding) <paramref name="baseFinal"/>.</summary>
    private static bool HasReparsePointBetween(string baseFinal, string start)
    {
        string? current = Path.GetDirectoryName(start);
        while (current is not null && !PathEquals(current, baseFinal) && IsInside(current, baseFinal))
        {
            if (PathResolver.IsReparsePoint(current))
            {
                return true;
            }

            current = Path.GetDirectoryName(current);
        }

        return false;
    }

    private static bool HasDangerousForm(string path)
    {
        if (path.Contains('\0', StringComparison.Ordinal))
        {
            return true;
        }

        if (path.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            return true; // device path
        }

        if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            string rest = path[4..];
            if (rest.StartsWith("GLOBALROOT", StringComparison.OrdinalIgnoreCase) ||
                rest.StartsWith(@"UNC\", StringComparison.OrdinalIgnoreCase))
            {
                return true; // device namespace or extended UNC
            }
            // \\?\C:\... long-path form is allowed; fall through.
        }
        else if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return true; // UNC / network path
        }

        // Alternate data stream: a ':' anywhere other than the drive-letter colon at index 1.
        int firstColon = path.IndexOf(':', StringComparison.Ordinal);
        if (firstColon >= 0)
        {
            int extra = path.IndexOf(':', firstColon + 1);
            if (extra >= 0 || firstColon != 1)
            {
                // Account for the \\?\C:\ form where the colon is at index 5.
                if (!(path.StartsWith(@"\\?\", StringComparison.Ordinal) && firstColon == 5 && path.IndexOf(':', 6) < 0))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string Normalize(string path)
        => Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    private static bool PathEquals(string a, string b)
        => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private bool IsTooBroadBase(string baseFinal)
    {
        string? root = Path.GetPathRoot(baseFinal);
        if (root is not null && PathEquals(Path.TrimEndingDirectorySeparator(root), baseFinal))
        {
            return true; // a drive root
        }

        foreach (string container in _containerRoots)
        {
            if (PathEquals(baseFinal, container))
            {
                return true; // a protected container root (Windows, ProgramData, profile, AppData, …)
            }
        }

        return false;
    }

    private static bool IsInside(string child, string parent)
    {
        if (child.Length <= parent.Length)
        {
            return false;
        }

        if (!child.StartsWith(parent, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        char boundary = child[parent.Length];
        return boundary is '\\' or '/';
    }
}
