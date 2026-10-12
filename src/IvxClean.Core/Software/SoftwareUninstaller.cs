using System.Diagnostics;
using Windows.Management.Deployment;

namespace IvxClean.Core.Software;

/// <summary>The result of launching an uninstall.</summary>
public sealed record UninstallResult(bool Started, string Message);

/// <summary>Launches a program's own uninstaller (one at a time, never silently without the user's
/// confirmation, which happens in the UI). Never removes provisioned packages for all users.</summary>
public interface ISoftwareUninstaller
{
    /// <summary>Launches the uninstall for a single entry and waits for it to finish.</summary>
    Task<UninstallResult> UninstallAsync(SoftwareEntry entry, CancellationToken cancellationToken);
}

/// <inheritdoc cref="ISoftwareUninstaller"/>
public sealed class ProcessSoftwareUninstaller : ISoftwareUninstaller
{
    /// <inheritdoc />
    public async Task<UninstallResult> UninstallAsync(SoftwareEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (entry.Classification == SoftwareClassification.NeverList)
        {
            return new UninstallResult(false, "This item is protected and cannot be uninstalled from IVX Clean.");
        }

        if (entry.Raw.Source == SoftwareSource.AppX)
        {
            return await UninstallAppxAsync(entry, cancellationToken).ConfigureAwait(false);
        }

        string? commandLine = entry.Raw.QuietUninstallString ?? entry.Raw.UninstallString;
        UninstallCommand? command = UninstallCommand.Parse(commandLine);
        if (command is null)
        {
            return new UninstallResult(false, "This program does not provide an uninstaller command.");
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = command.FileName,
            UseShellExecute = true, // let the vendor's uninstaller show its own UI
        };
        foreach (string arg in command.Arguments)
        {
            startInfo.ArgumentList.Add(arg);
        }

        try
        {
            using Process? process = Process.Start(startInfo);
            if (process is null)
            {
                return new UninstallResult(false, "The uninstaller could not be started.");
            }

            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            return new UninstallResult(true, $"Uninstaller finished (exit code {process.ExitCode}).");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return new UninstallResult(false, $"The uninstaller could not be started: {ex.Message}");
        }
    }

    private static async Task<UninstallResult> UninstallAppxAsync(SoftwareEntry entry, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(entry.Raw.PackageFullName))
        {
            return new UninstallResult(false, "This Store app has no package identity to remove.");
        }

        try
        {
            var manager = new PackageManager();
            // Current user only — never remove provisioned packages for all users.
            DeploymentResult result = await manager
                .RemovePackageAsync(entry.Raw.PackageFullName, RemovalOptions.None)
                .AsTask(cancellationToken)
                .ConfigureAwait(false);

            return string.IsNullOrEmpty(result.ErrorText)
                ? new UninstallResult(true, "Store app removed.")
                : new UninstallResult(false, result.ErrorText);
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or System.Runtime.InteropServices.COMException)
        {
            return new UninstallResult(false, $"The Store app could not be removed: {ex.Message}");
        }
    }
}
