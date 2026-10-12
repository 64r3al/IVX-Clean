using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace IvxClean.Core.Interop;

/// <summary>
/// Reparse-point detection and final-path resolution, used by the SafetyGuard to defend
/// against junction/symlink/mount-point redirection attacks.
/// </summary>
public static class PathResolver
{
    /// <summary>
    /// Returns <see langword="true"/> if the item at <paramref name="path"/> exists and is a
    /// reparse point (junction, symlink or mount point). Returns <see langword="false"/> if the
    /// item does not exist or its attributes cannot be read.
    /// </summary>
    public static bool IsReparsePoint(string path)
    {
        try
        {
            FileAttributes attributes = File.GetAttributes(path);
            return (attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Resolves <paramref name="path"/> to its canonical final path, following any reparse
    /// points along the way. Returns <see langword="false"/> if the path cannot be opened or
    /// resolved — in which case the caller must treat it as unsafe and skip it.
    /// </summary>
    public static bool TryGetFinalPath(string path, out string finalPath)
    {
        finalPath = string.Empty;
        SafeFileHandle? handle = null;
        try
        {
            // Open following reparse points (no OPEN_REPARSE_POINT) with backup semantics so
            // directories can be opened too. We only need FILE_READ_ATTRIBUTES.
            handle = NativeMethods.CreateFile(
                ToExtendedLengthPath(path),
                NativeMethods.FILE_READ_ATTRIBUTES,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE | NativeMethods.FILE_SHARE_DELETE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                NativeMethods.FILE_FLAG_BACKUP_SEMANTICS,
                IntPtr.Zero);

            if (handle.IsInvalid)
            {
                return false;
            }

            char[] buffer = new char[1024];
            uint length = NativeMethods.GetFinalPathNameByHandle(
                handle, buffer, (uint)buffer.Length, NativeMethods.FILE_NAME_NORMALIZED | NativeMethods.VOLUME_NAME_DOS);

            if (length == 0)
            {
                return false;
            }

            if (length > buffer.Length)
            {
                buffer = new char[length];
                length = NativeMethods.GetFinalPathNameByHandle(
                    handle, buffer, (uint)buffer.Length, NativeMethods.FILE_NAME_NORMALIZED | NativeMethods.VOLUME_NAME_DOS);
                if (length == 0 || length > buffer.Length)
                {
                    return false;
                }
            }

            string resolved = new(buffer, 0, (int)length);
            finalPath = StripExtendedPrefix(resolved);
            return true;
        }
        catch (Win32Exception)
        {
            return false;
        }
        finally
        {
            handle?.Dispose();
        }
    }

    /// <summary>
    /// Adds the <c>\\?\</c> extended-length prefix so <c>CreateFile</c> accepts paths longer than
    /// MAX_PATH regardless of the process's long-path awareness. Expects an already-normalized,
    /// fully-qualified path (the SafetyGuard normalizes before calling).
    /// </summary>
    private static string ToExtendedLengthPath(string path)
    {
        if (path.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return path;
        }

        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return @"\\?\UNC\" + path[2..];
        }

        return @"\\?\" + path;
    }

    /// <summary>
    /// Removes the <c>\\?\</c> / <c>\\?\UNC\</c> long-path prefix from a normalized path so it
    /// compares cleanly against ordinary paths. A UNC final path is returned as <c>\\server\...</c>.
    /// </summary>
    private static string StripExtendedPrefix(string path)
    {
        const string uncPrefix = @"\\?\UNC\";
        const string dosPrefix = @"\\?\";

        if (path.StartsWith(uncPrefix, StringComparison.Ordinal))
        {
            return @"\\" + path[uncPrefix.Length..];
        }

        if (path.StartsWith(dosPrefix, StringComparison.Ordinal))
        {
            return path[dosPrefix.Length..];
        }

        return path;
    }
}
