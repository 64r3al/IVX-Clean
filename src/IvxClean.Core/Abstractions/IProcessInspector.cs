namespace IvxClean.Core.Abstractions;

/// <summary>
/// Reports which processes are currently running, so rules with <c>requiresClosed</c> can mark
/// locked items as skipped. IVX Clean never kills processes.
/// </summary>
public interface IProcessInspector
{
    /// <summary>
    /// Returns the set of running process executable names, lower-cased and including the
    /// <c>.exe</c> extension (e.g. <c>chrome.exe</c>).
    /// </summary>
    IReadOnlySet<string> RunningExecutables();
}

/// <summary>Production process inspector backed by the OS process list.</summary>
public sealed class ProcessInspector : IProcessInspector
{
    /// <inheritdoc />
    public IReadOnlySet<string> RunningExecutables()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (System.Diagnostics.Process process in System.Diagnostics.Process.GetProcesses())
        {
            try
            {
                string name = process.ProcessName;
                if (name.Length > 0)
                {
                    names.Add(name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? name : name + ".exe");
                }
            }
            catch (InvalidOperationException)
            {
                // Process exited between enumeration and read; ignore.
            }
            finally
            {
                process.Dispose();
            }
        }

        return names;
    }
}
