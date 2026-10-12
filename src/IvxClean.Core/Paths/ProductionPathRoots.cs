using IvxClean.Core.Interop;
using Microsoft.Win32;

namespace IvxClean.Core.Paths;

/// <summary>
/// Resolves real system locations on the running machine. All values are resolved once and
/// cached; the cleaner and rules consume them only through <see cref="IPathRoots"/>.
/// </summary>
public sealed class ProductionPathRoots : PathRootsBase
{
    private readonly Lazy<IReadOnlyList<string>> _eachUserProfile;
    private readonly Lazy<IReadOnlyList<string>> _eachFixedDrive;

    /// <summary>Creates the production path roots, reading from the environment and registry.</summary>
    public ProductionPathRoots()
    {
        Temp = Path.TrimEndingDirectorySeparator(Path.GetTempPath());
        LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        RoamingAppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        UserProfile = KnownFolders.TryGetProfile(out string profile)
            ? profile
            : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        LocalLowAppData = Path.Combine(UserProfile, "AppData", "LocalLow");
        ProgramData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        Windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        SystemDrive = Path.GetPathRoot(Windows) ?? @"C:\";
        AppDataRoot = Path.Combine(LocalAppData, "IVXClean");
        AppInstallDirectory = Path.TrimEndingDirectorySeparator(AppContext.BaseDirectory);

        _eachUserProfile = new Lazy<IReadOnlyList<string>>(ResolveUserProfiles);
        _eachFixedDrive = new Lazy<IReadOnlyList<string>>(ResolveFixedDrives);
    }

    /// <inheritdoc />
    public override string Temp { get; }

    /// <inheritdoc />
    public override string LocalAppData { get; }

    /// <inheritdoc />
    public override string RoamingAppData { get; }

    /// <inheritdoc />
    public override string LocalLowAppData { get; }

    /// <inheritdoc />
    public override string ProgramData { get; }

    /// <inheritdoc />
    public override string Windows { get; }

    /// <inheritdoc />
    public override string SystemDrive { get; }

    /// <inheritdoc />
    public override string UserProfile { get; }

    /// <inheritdoc />
    public override string AppDataRoot { get; }

    /// <inheritdoc />
    public override string AppInstallDirectory { get; }

    /// <inheritdoc />
    public override IReadOnlyList<string> EachUserProfile => _eachUserProfile.Value;

    /// <inheritdoc />
    public override IReadOnlyList<string> EachFixedDrive => _eachFixedDrive.Value;

    private static List<string> ResolveUserProfiles()
    {
        var profiles = new List<string>();
        try
        {
            using RegistryKey? list = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList");
            if (list is null)
            {
                return profiles;
            }

            foreach (string sid in list.GetSubKeyNames())
            {
                // Only real interactive accounts (S-1-5-21-...). This excludes the SYSTEM (18),
                // LOCAL SERVICE (19) and NETWORK SERVICE (20) profiles automatically.
                if (!sid.StartsWith("S-1-5-21-", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                using RegistryKey? entry = list.OpenSubKey(sid);
                if (entry?.GetValue("ProfileImagePath") is not string imagePath || imagePath.Length == 0)
                {
                    continue;
                }

                string name = Path.GetFileName(Path.TrimEndingDirectorySeparator(imagePath));
                if (name.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Default User", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("systemprofile", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (Directory.Exists(imagePath))
                {
                    profiles.Add(Path.TrimEndingDirectorySeparator(imagePath));
                }
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // Fail safe: an unreadable profile list simply yields no "each user" expansion.
        }

        return profiles;
    }

    private static List<string> ResolveFixedDrives()
    {
        var drives = new List<string>();
        foreach (DriveInfo drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType == DriveType.Fixed && drive.IsReady)
                {
                    drives.Add(drive.RootDirectory.FullName);
                }
            }
            catch (IOException)
            {
                // Skip a drive that disappears or errors mid-enumeration.
            }
        }

        return drives;
    }
}
