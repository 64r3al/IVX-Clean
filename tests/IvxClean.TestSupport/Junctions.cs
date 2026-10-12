using System.Diagnostics;

namespace IvxClean.TestSupport;

/// <summary>
/// Creates NTFS directory junctions for tests. Junctions (unlike symlinks) do not require
/// administrator rights, so the junction-attack test runs on CI.
/// </summary>
public static class Junctions
{
    /// <summary>
    /// Creates a directory junction at <paramref name="linkPath"/> pointing at
    /// <paramref name="targetPath"/>. Returns <see langword="true"/> on success.
    /// </summary>
    public static bool TryCreate(string linkPath, string targetPath)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        startInfo.ArgumentList.Add("/c");
        startInfo.ArgumentList.Add("mklink");
        startInfo.ArgumentList.Add("/J");
        startInfo.ArgumentList.Add(linkPath);
        startInfo.ArgumentList.Add(targetPath);

        using Process? process = Process.Start(startInfo);
        if (process is null)
        {
            return false;
        }

        process.WaitForExit(10_000);
        return process.HasExited && process.ExitCode == 0 && Directory.Exists(linkPath);
    }
}
