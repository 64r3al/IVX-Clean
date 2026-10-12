using IvxClean.Core.Paths;

namespace IvxClean.TestSupport;

/// <summary>
/// An <see cref="IPathRoots"/> whose every root lives inside a throwaway per-test sandbox folder.
/// Nothing it resolves ever points at a real system location. Dispose deletes the sandbox.
/// </summary>
public sealed class SandboxPathRoots : PathRootsBase, IDisposable
{
    /// <summary>Creates and lays out a fresh sandbox under the OS temp folder.</summary>
    public SandboxPathRoots()
    {
        SandboxRoot = Path.Combine(Path.GetTempPath(), "ivxclean-sbx-" + Guid.NewGuid().ToString("N"));

        Temp = Dir("Temp");
        LocalAppData = Dir("LocalAppData");
        RoamingAppData = Dir("RoamingAppData");
        LocalLowAppData = Dir("LocalLowAppData");
        ProgramData = Dir("ProgramData");
        Windows = Dir("Windows");
        UserProfile = Dir(Path.Combine("Users", "tester"));
        AppInstallDirectory = Dir(Path.Combine("Program Files", "IVX Clean"));
        AppDataRoot = Dir(Path.Combine("LocalAppData", "IVXClean"));
        SystemDrive = EnsureDir(SandboxRoot);

        EachUserProfile = [UserProfile];
        EachFixedDrive = [SandboxRoot];
    }

    /// <summary>The root of this sandbox.</summary>
    public string SandboxRoot { get; }

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
    public override IReadOnlyList<string> EachUserProfile { get; }

    /// <inheritdoc />
    public override IReadOnlyList<string> EachFixedDrive { get; }

    /// <summary>Creates a directory under the sandbox and returns its absolute path.</summary>
    public string Dir(string relative) => EnsureDir(Path.Combine(SandboxRoot, relative));

    /// <summary>Creates a file under the sandbox with the given contents and last-write time.</summary>
    public string File(string relative, string contents = "x", DateTime? lastWriteUtc = null)
    {
        string full = Path.GetFullPath(Path.Combine(SandboxRoot, relative));
        EnsureDir(Path.GetDirectoryName(full)!);
        System.IO.File.WriteAllText(full, contents);
        if (lastWriteUtc is { } when)
        {
            System.IO.File.SetLastWriteTimeUtc(full, when);
        }

        return full;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(SandboxRoot))
            {
                Directory.Delete(SandboxRoot, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Best-effort cleanup; a locked file left behind must not fail the test.
        }
    }

    private static string EnsureDir(string path)
    {
        string full = Path.GetFullPath(path);
        Directory.CreateDirectory(full);
        return Path.TrimEndingDirectorySeparator(full);
    }
}
