using System.Runtime.InteropServices;
using IvxClean.Core.Interop;

namespace IvxClean.Core.Software;

/// <summary>A parsed uninstall command: an executable and its argument list, ready to launch
/// without going through a shell.</summary>
public sealed record UninstallCommand(string FileName, IReadOnlyList<string> Arguments)
{
    /// <summary>
    /// Parses a registry <c>UninstallString</c> into an executable and arguments, using
    /// <c>CommandLineToArgvW</c> semantics (quoted paths, embedded spaces). An MSI uninstall of the
    /// form <c>MsiExec /I{GUID}</c> or <c>/X{GUID}</c> is normalised to a silent-safe <c>/X</c> form.
    /// Returns <see langword="null"/> if the string is empty or cannot be parsed.
    /// </summary>
    public static UninstallCommand? Parse(string? uninstallString)
    {
        if (string.IsNullOrWhiteSpace(uninstallString))
        {
            return null;
        }

        string[] tokens = Tokenize(uninstallString.Trim());
        if (tokens.Length == 0)
        {
            return null;
        }

        string fileName = tokens[0];
        List<string> args = [.. tokens[1..]];

        // Normalise MsiExec /I{GUID} (install) to /X{GUID} (uninstall).
        if (Path.GetFileNameWithoutExtension(fileName).Equals("msiexec", StringComparison.OrdinalIgnoreCase))
        {
            for (int i = 0; i < args.Count; i++)
            {
                if (args[i].StartsWith("/I", StringComparison.OrdinalIgnoreCase) && LooksLikeGuidArg(args[i]))
                {
                    args[i] = "/X" + args[i][2..];
                }
            }
        }

        return new UninstallCommand(fileName, args);
    }

    private static bool LooksLikeGuidArg(string arg)
        => arg.Length > 2 && arg[2] == '{';

    private static string[] Tokenize(string commandLine)
    {
        IntPtr argv = NativeMethods.CommandLineToArgv(commandLine, out int count);
        if (argv == IntPtr.Zero || count <= 0)
        {
            return [];
        }

        try
        {
            var result = new string[count];
            for (int i = 0; i < count; i++)
            {
                IntPtr ptr = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                result[i] = Marshal.PtrToStringUni(ptr) ?? string.Empty;
            }

            return result;
        }
        finally
        {
            _ = NativeMethods.LocalFree(argv);
        }
    }
}
