using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace IvxClean.Core.Software;

/// <summary>Reads Store / AppX packages installed for the current user, excluding frameworks,
/// resource packages and system-signed packages (which are never offered for removal).</summary>
public sealed class AppxInstalledSoftwareSource : IInstalledSoftwareSource
{
    /// <inheritdoc />
    public IReadOnlyList<RawSoftwareEntry> GetRaw()
    {
        var entries = new List<RawSoftwareEntry>();
        PackageManager manager;
        try
        {
            manager = new PackageManager();
        }
        catch (Exception ex) when (ex is TypeInitializationException or PlatformNotSupportedException or UnauthorizedAccessException)
        {
            return entries;
        }

        IEnumerable<Package> packages;
        try
        {
            packages = manager.FindPackagesForUser(string.Empty);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or ArgumentException)
        {
            return entries;
        }

        foreach (Package package in packages)
        {
            try
            {
                if (package.IsFramework || package.IsResourcePackage || package.SignatureKind == PackageSignatureKind.System)
                {
                    continue;
                }

                entries.Add(new RawSoftwareEntry
                {
                    Id = package.Id.FullName,
                    DisplayName = string.IsNullOrWhiteSpace(package.DisplayName) ? package.Id.Name : package.DisplayName,
                    Publisher = package.PublisherDisplayName ?? string.Empty,
                    Version = $"{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}",
                    InstallDateUtc = SafeInstalledDate(package),
                    PackageFullName = package.Id.FullName,
                    Source = SoftwareSource.AppX,
                });
            }
            catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
            {
                // Skip a package whose metadata cannot be read.
            }
        }

        return entries;
    }

    private static DateTime? SafeInstalledDate(Package package)
    {
        try
        {
            return package.InstalledDate.UtcDateTime;
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Runtime.InteropServices.COMException)
        {
            return null;
        }
    }
}
