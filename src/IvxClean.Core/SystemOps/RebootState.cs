using Microsoft.Win32;

namespace IvxClean.Core.SystemOps;

/// <summary>Reports whether Windows is mid-update or waiting for a reboot, so the Windows Update
/// cache rule can refuse to run at an unsafe time.</summary>
public interface IRebootState
{
    /// <summary>True if a reboot is pending or an update/servicing operation is in progress.</summary>
    bool IsRebootPendingOrServicing();
}

/// <inheritdoc cref="IRebootState"/>
public sealed class RegistryRebootState : IRebootState
{
    private readonly Abstractions.IServiceController _services;

    /// <summary>Creates the reboot-state probe.</summary>
    public RegistryRebootState(Abstractions.IServiceController services)
        => _services = services ?? throw new ArgumentNullException(nameof(services));

    /// <inheritdoc />
    public bool IsRebootPendingOrServicing()
    {
        if (KeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Component Based Servicing\RebootPending") ||
            KeyExists(@"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired"))
        {
            return true;
        }

        // TrustedInstaller actively running means servicing is in progress.
        return _services.GetState("TrustedInstaller") == Abstractions.ServiceRunState.Running;
    }

    private static bool KeyExists(string subKey)
    {
        try
        {
            using RegistryKey? key = Registry.LocalMachine.OpenSubKey(subKey);
            return key is not null;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        {
            // If we cannot tell, assume servicing to stay on the safe side.
            return true;
        }
    }
}
