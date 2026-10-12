using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace IvxClean.Core.Interop;

/// <summary>
/// Minimal, carefully scoped P/Invoke surface. Every entry point uses <see cref="SafeHandle"/>s,
/// UTF-16 marshalling and <c>SetLastError</c> where the API reports errors that way.
/// </summary>
internal static partial class NativeMethods
{
    // --- CreateFile ---------------------------------------------------------
    internal const uint FILE_READ_ATTRIBUTES = 0x0080;
    internal const uint FILE_SHARE_READ = 0x00000001;
    internal const uint FILE_SHARE_WRITE = 0x00000002;
    internal const uint FILE_SHARE_DELETE = 0x00000004;
    internal const uint OPEN_EXISTING = 3;
    internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000; // required to open a directory handle
    internal const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial SafeFileHandle CreateFile(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    // --- GetFinalPathNameByHandle ------------------------------------------
    internal const uint FILE_NAME_NORMALIZED = 0x0;
    internal const uint VOLUME_NAME_DOS = 0x0;

    [LibraryImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial uint GetFinalPathNameByHandle(
        SafeFileHandle hFile,
        [Out] char[] lpszFilePath,
        uint cchFilePath,
        uint dwFlags);

    // --- SHGetKnownFolderPath ----------------------------------------------
    [LibraryImport("shell32.dll", EntryPoint = "SHGetKnownFolderPath")]
    internal static partial int SHGetKnownFolderPath(
        in Guid rfid,
        uint dwFlags,
        IntPtr hToken,
        out IntPtr ppszPath);

    // --- Recycle Bin -------------------------------------------------------
    internal const uint SHERB_NOCONFIRMATION = 0x00000001;
    internal const uint SHERB_NOPROGRESSUI = 0x00000002;
    internal const uint SHERB_NOSOUND = 0x00000004;

    [StructLayout(LayoutKind.Sequential)]
    internal struct SHQUERYRBINFO
    {
        internal uint cbSize;
        internal long i64Size;
        internal long i64NumItems;
    }

    [LibraryImport("shell32.dll", EntryPoint = "SHQueryRecycleBinW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int SHQueryRecycleBin(string? pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

    [LibraryImport("shell32.dll", EntryPoint = "SHEmptyRecycleBinW", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int SHEmptyRecycleBin(IntPtr hwnd, string? pszRootPath, uint dwFlags);
}
