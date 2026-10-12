using System.Diagnostics;

namespace IvxClean.Core.SystemOps;

/// <summary>The outcome of a DISM run.</summary>
public sealed record DismResult(bool Success, int ExitCode);

/// <summary>Runs DISM component-store cleanup (Expert). Never uses /ResetBase.</summary>
public interface IDismRunner
{
    /// <summary>Runs <c>DISM /Online /Cleanup-Image /StartComponentCleanup</c>, reporting output lines.</summary>
    Task<DismResult> RunComponentCleanupAsync(IProgress<string>? progress, CancellationToken cancellationToken);
}

/// <inheritdoc cref="IDismRunner"/>
public sealed class DismRunner : IDismRunner
{
    /// <inheritdoc />
    public async Task<DismResult> RunComponentCleanupAsync(IProgress<string>? progress, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "Dism.exe"),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        // Fixed, trusted arguments only — never concatenated from external input, never /ResetBase.
        startInfo.ArgumentList.Add("/Online");
        startInfo.ArgumentList.Add("/Cleanup-Image");
        startInfo.ArgumentList.Add("/StartComponentCleanup");

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => { if (e.Data is not null) { progress?.Report(e.Data); } };
        process.ErrorDataReceived += (_, e) => { if (e.Data is not null) { progress?.Report(e.Data); } };

        if (!process.Start())
        {
            return new DismResult(false, -1);
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        return new DismResult(process.ExitCode == 0, process.ExitCode);
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Nothing more we can safely do.
        }
    }
}
