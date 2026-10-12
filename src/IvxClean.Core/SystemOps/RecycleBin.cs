using IvxClean.Core.Interop;

namespace IvxClean.Core.SystemOps;

/// <summary>Reads and empties the Recycle Bin via the Shell API.</summary>
public interface IRecycleBin
{
    /// <summary>Returns the total size in bytes of the Recycle Bin across all drives.</summary>
    long GetSize();

    /// <summary>Empties the Recycle Bin on all drives, with no prompts, sound or progress UI.</summary>
    void Empty();
}

/// <inheritdoc cref="IRecycleBin"/>
public sealed class ShellRecycleBin : IRecycleBin
{
    /// <inheritdoc />
    public long GetSize()
    {
        var info = new NativeMethods.SHQUERYRBINFO { cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.SHQUERYRBINFO>() };
        int hr = NativeMethods.SHQueryRecycleBin(null, ref info);
        return hr == 0 ? info.i64Size : 0;
    }

    /// <inheritdoc />
    public void Empty()
    {
        uint flags = NativeMethods.SHERB_NOCONFIRMATION | NativeMethods.SHERB_NOPROGRESSUI | NativeMethods.SHERB_NOSOUND;
        _ = NativeMethods.SHEmptyRecycleBin(IntPtr.Zero, null, flags);
    }
}
