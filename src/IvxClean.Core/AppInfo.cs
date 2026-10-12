namespace IvxClean.Core;

/// <summary>
/// Static product metadata shared across all IVX Clean processes (app, CLI, tools).
/// </summary>
public static class AppInfo
{
    /// <summary>The product display name.</summary>
    public const string Name = "IVX Clean";

    /// <summary>The author/vendor credited throughout the product.</summary>
    public const string MadeBy = "IVX";

    /// <summary>The public GitHub repository, used by the user-initiated update check.</summary>
    public const string RepositoryUrl = "https://github.com/64r3al/IVX-Clean";

    /// <summary>
    /// The product version, resolved from the executing assembly's informational version
    /// (set by the build/release pipeline), falling back to the assembly version.
    /// </summary>
    public static string Version
    {
        get
        {
            System.Reflection.Assembly assembly = typeof(AppInfo).Assembly;
            string? informational = assembly
                .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
                .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
                .FirstOrDefault()?.InformationalVersion;

            // Strip any source-control metadata suffix (e.g. "1.0.0+<sha>").
            if (!string.IsNullOrWhiteSpace(informational))
            {
                int plus = informational.IndexOf('+', StringComparison.Ordinal);
                return plus >= 0 ? informational[..plus] : informational;
            }

            return assembly.GetName().Version?.ToString(3) ?? "1.0.0";
        }
    }
}
