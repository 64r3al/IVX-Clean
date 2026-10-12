using System.Runtime.InteropServices;

namespace IvxClean.Core.Interop;

/// <summary>
/// Resolves Windows Known Folders via <c>SHGetKnownFolderPath</c>, including user-redirected and
/// OneDrive-backed locations. Used to build the SafetyGuard's protected-user-folder set.
/// </summary>
public static class KnownFolders
{
    // KNOWNFOLDERID GUIDs (shlobj.h).
    private static readonly Guid Desktop = new("B4BFCC3A-DB2C-424C-B029-7FE99A87C641");
    private static readonly Guid Documents = new("FDD39AD0-238F-46AF-ADB4-6C85480369C7");
    private static readonly Guid Downloads = new("374DE290-123F-4565-9164-39C4925E467B");
    private static readonly Guid Pictures = new("33E28130-4E1E-4676-835A-98395C3BC3BB");
    private static readonly Guid Videos = new("18989B1D-99B5-455B-841C-AB7C74E4DDFC");
    private static readonly Guid Music = new("4BD8D571-6D19-48D3-BE97-422220080E43");
    private static readonly Guid SavedGames = new("4C5C32FF-BB9D-43B0-B5B4-2D72E54EAAA4");
    private static readonly Guid Favorites = new("1777F761-68AD-4D8A-87BD-30B759FA33DD");
    private static readonly Guid Contacts = new("56784854-C6CB-462B-8169-88E350ACB882");
    private static readonly Guid Links = new("BFB9D5E0-C6A9-404C-B2B2-AE6DB6AF4968");
    private static readonly Guid Searches = new("7D1D3A04-DEBB-4115-95CF-2F29DA2920DA");
    private static readonly Guid Objects3D = new("31C0DD25-9439-4F12-BF41-7FF4EDA38722");
    private static readonly Guid CameraRoll = new("AB5FB87B-7CE2-4F83-915D-550846C9537B");
    private static readonly Guid Screenshots = new("B7BEDE81-DF94-4682-A7D8-57A52620B86F");
    private static readonly Guid Profile = new("5E6C858F-0E22-4760-9AFE-EA3317B67173");
    private static readonly Guid SkyDrive = new("A52BBA46-E9E1-435F-B3D9-28DAA648C0F6");

    private static readonly Guid[] ProtectedFolderIds =
    [
        Desktop, Documents, Downloads, Pictures, Videos, Music, SavedGames,
        Favorites, Contacts, Links, Searches, Objects3D, CameraRoll, Screenshots, SkyDrive,
    ];

    /// <summary>
    /// Resolves a single Known Folder path for the current user. Returns <see langword="false"/>
    /// if the folder is not defined on this machine.
    /// </summary>
    public static bool TryGetPath(Guid folderId, out string path)
    {
        path = string.Empty;
        IntPtr pszPath = IntPtr.Zero;
        try
        {
            int hr = NativeMethods.SHGetKnownFolderPath(in folderId, 0, IntPtr.Zero, out pszPath);
            if (hr != 0 || pszPath == IntPtr.Zero)
            {
                return false;
            }

            string? resolved = Marshal.PtrToStringUni(pszPath);
            if (string.IsNullOrWhiteSpace(resolved))
            {
                return false;
            }

            path = resolved;
            return true;
        }
        finally
        {
            if (pszPath != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pszPath);
            }
        }
    }

    /// <summary>The current user's profile root, resolved via the Known Folder API.</summary>
    public static bool TryGetProfile(out string path) => TryGetPath(Profile, out path);

    /// <summary>
    /// Returns every protected user folder that is defined on this machine (Desktop, Documents,
    /// Downloads, Pictures, Videos, Music, Saved Games, Favorites, Contacts, Links, Searches,
    /// 3D Objects, Camera Roll, Screenshots and the OneDrive root), plus the OneDrive roots from
    /// environment variables. Folders that are not defined are simply omitted.
    /// </summary>
    public static IReadOnlyList<string> GetProtectedUserFolders()
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Guid id in ProtectedFolderIds)
        {
            if (TryGetPath(id, out string p) && p.Length > 0)
            {
                set.Add(p);
            }
        }

        foreach (string variable in (string[])["OneDrive", "OneDriveConsumer", "OneDriveCommercial"])
        {
            string? value = Environment.GetEnvironmentVariable(variable);
            if (!string.IsNullOrWhiteSpace(value))
            {
                set.Add(value);
            }
        }

        return [.. set];
    }
}
