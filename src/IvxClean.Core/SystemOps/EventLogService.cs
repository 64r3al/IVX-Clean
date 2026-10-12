using System.Diagnostics;

namespace IvxClean.Core.SystemOps;

/// <summary>Clears Windows event logs (Expert, off by default).</summary>
public interface IEventLogService
{
    /// <summary>Clears the main event logs. Returns the number of logs cleared.</summary>
    int ClearStandardLogs();
}

/// <inheritdoc cref="IEventLogService"/>
public sealed class WindowsEventLogService : IEventLogService
{
    private static readonly string[] StandardLogs = ["Application", "System", "Security", "Setup"];

    /// <inheritdoc />
    public int ClearStandardLogs()
    {
        int cleared = 0;
        foreach (string log in StandardLogs)
        {
            try
            {
                if (EventLog.Exists(log))
                {
                    using var eventLog = new EventLog(log);
                    eventLog.Clear();
                    cleared++;
                }
            }
            catch (Exception ex) when (ex is System.Security.SecurityException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
                // A log we cannot clear (e.g. Security without rights) is skipped, not fatal.
            }
        }

        return cleared;
    }
}
