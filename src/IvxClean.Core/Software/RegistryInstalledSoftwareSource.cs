using System.Globalization;
using Microsoft.Win32;

namespace IvxClean.Core.Software;

/// <summary>Reads installed programs from the three uninstall registry locations
/// (HKLM 64-bit, HKLM WOW6432Node, HKCU) and applies the inventory filter.</summary>
public sealed class RegistryInstalledSoftwareSource : IInstalledSoftwareSource
{
    private const string UninstallPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    /// <inheritdoc />
    public IReadOnlyList<RawSoftwareEntry> GetRaw()
    {
        var entries = new List<RawSoftwareEntry>();
        ReadHive(RegistryHive.LocalMachine, RegistryView.Registry64, entries);
        ReadHive(RegistryHive.LocalMachine, RegistryView.Registry32, entries);
        ReadHive(RegistryHive.CurrentUser, RegistryView.Default, entries);
        return entries;
    }

    private static void ReadHive(RegistryHive hive, RegistryView view, List<RawSoftwareEntry> into)
    {
        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(hive, view);
            using RegistryKey? uninstall = baseKey.OpenSubKey(UninstallPath);
            if (uninstall is null)
            {
                return;
            }

            foreach (string subKeyName in uninstall.GetSubKeyNames())
            {
                using RegistryKey? app = uninstall.OpenSubKey(subKeyName);
                if (app is null)
                {
                    continue;
                }

                RegistryUninstallRecord record = ReadRecord(app, $"{hive}\\{view}\\{subKeyName}");
                if (SoftwareInventoryFilter.ShouldInclude(record))
                {
                    into.Add(SoftwareInventoryFilter.ToRawEntry(record));
                }
            }
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // An unreadable hive simply contributes nothing.
        }
    }

    private static RegistryUninstallRecord ReadRecord(RegistryKey app, string keyId)
        => new()
        {
            KeyId = keyId,
            DisplayName = app.GetValue("DisplayName") as string,
            Publisher = app.GetValue("Publisher") as string,
            Version = app.GetValue("DisplayVersion") as string,
            SystemComponent = app.GetValue("SystemComponent") as int?,
            ParentKeyName = app.GetValue("ParentKeyName") as string,
            ReleaseType = app.GetValue("ReleaseType") as string,
            WindowsInstaller = app.GetValue("WindowsInstaller") as int?,
            UninstallString = app.GetValue("UninstallString") as string,
            QuietUninstallString = app.GetValue("QuietUninstallString") as string,
            EstimatedSizeKb = Convert.ToInt64(app.GetValue("EstimatedSize") ?? 0, CultureInfo.InvariantCulture),
            InstallDateUtc = ParseInstallDate(app.GetValue("InstallDate") as string),
        };

    private static DateTime? ParseInstallDate(string? value)
        => DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out DateTime date)
            ? date
            : null;
}
