using System.Globalization;

namespace IvxClean.Core.Formatting;

/// <summary>Formats byte counts for display using the current culture (1 decimal place).</summary>
public static class ByteSize
{
    private static readonly string[] Units = ["bytes", "KB", "MB", "GB", "TB", "PB"];

    /// <summary>Returns a human-readable size such as <c>7.0 GB</c> or <c>512 bytes</c>.</summary>
    public static string Humanize(long bytes, CultureInfo? culture = null)
    {
        culture ??= CultureInfo.CurrentCulture;
        if (bytes < 1024)
        {
            return string.Format(culture, "{0} {1}", bytes, Units[0]);
        }

        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return string.Format(culture, "{0:0.0} {1}", value, Units[unit]);
    }
}
