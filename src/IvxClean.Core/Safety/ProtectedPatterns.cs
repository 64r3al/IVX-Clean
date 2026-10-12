using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace IvxClean.Core.Safety;

/// <summary>
/// The set of file and folder patterns that are never deleted, anywhere — autosave, recovery,
/// backup and creative project files, Office owner/lock files, and protected folder names.
/// Loaded from the embedded <c>rules/protected.json</c>. Over-protection is intentional.
/// </summary>
public sealed partial class ProtectedPatterns
{
    private readonly HashSet<string> _extensions;
    private readonly string[] _extensionPrefixes;
    private readonly HashSet<string> _fileNames;
    private readonly string[] _fileNamePrefixes;
    private readonly HashSet<string> _folderNames;

    private ProtectedPatterns(Model model)
    {
        _extensions = new HashSet<string>(model.Extensions ?? [], StringComparer.OrdinalIgnoreCase);
        _extensionPrefixes = (model.ExtensionPrefixes ?? []).Select(p => p.ToLowerInvariant()).ToArray();
        _fileNames = new HashSet<string>(model.FileNames ?? [], StringComparer.OrdinalIgnoreCase);
        _fileNamePrefixes = (model.FileNamePrefixes ?? []).ToArray();
        _folderNames = new HashSet<string>(model.FolderNames ?? [], StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Loads the protected patterns from the catalog embedded in this assembly.</summary>
    public static ProtectedPatterns LoadEmbedded()
    {
        const string resourceName = "IvxClean.Core.Rules.protected.json";
        using Stream? stream = typeof(ProtectedPatterns).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
        Model? model = JsonSerializer.Deserialize(stream, ProtectedPatternsJsonContext.Default.Model);
        return new ProtectedPatterns(model ?? throw new InvalidOperationException("protected.json deserialized to null."));
    }

    /// <summary>Creates patterns directly (used by tests).</summary>
    internal static ProtectedPatterns FromValues(
        IEnumerable<string>? extensions = null,
        IEnumerable<string>? extensionPrefixes = null,
        IEnumerable<string>? fileNames = null,
        IEnumerable<string>? fileNamePrefixes = null,
        IEnumerable<string>? folderNames = null)
        => new(new Model
        {
            Extensions = extensions?.ToList(),
            ExtensionPrefixes = extensionPrefixes?.ToList(),
            FileNames = fileNames?.ToList(),
            FileNamePrefixes = fileNamePrefixes?.ToList(),
            FolderNames = folderNames?.ToList(),
        });

    /// <summary>The protected folder names (e.g. <c>AutoRecover</c>, <c>UnsavedFiles</c>).</summary>
    public IReadOnlySet<string> FolderNames => _folderNames;

    /// <summary>
    /// Returns <see langword="true"/> if the file name at the leaf of <paramref name="fullPath"/>
    /// matches a protected extension, extension-prefix, exact name or name-prefix.
    /// </summary>
    public bool MatchesFileName(string fullPath)
    {
        string name = Path.GetFileName(fullPath);
        if (name.Length == 0)
        {
            return false;
        }

        if (_fileNames.Contains(name))
        {
            return true;
        }

        foreach (string prefix in _fileNamePrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        string extension = Path.GetExtension(name);
        if (extension.Length > 0)
        {
            if (_extensions.Contains(extension))
            {
                return true;
            }

            foreach (string prefix in _extensionPrefixes)
            {
                if (extension.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Returns <see langword="true"/> if any directory segment of <paramref name="fullPath"/>
    /// (including the leaf when it is a directory) is a protected folder name.
    /// </summary>
    public bool MatchesProtectedFolder(string fullPath)
    {
        foreach (string segment in SplitSegments(fullPath))
        {
            if (_folderNames.Contains(segment))
            {
                return true;
            }
        }

        return false;
    }

    private static string[] SplitSegments(string fullPath)
        => fullPath.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);

    private sealed class Model
    {
        [JsonPropertyName("extensions")] public List<string>? Extensions { get; set; }

        [JsonPropertyName("extensionPrefixes")] public List<string>? ExtensionPrefixes { get; set; }

        [JsonPropertyName("fileNames")] public List<string>? FileNames { get; set; }

        [JsonPropertyName("fileNamePrefixes")] public List<string>? FileNamePrefixes { get; set; }

        [JsonPropertyName("folderNames")] public List<string>? FolderNames { get; set; }
    }

    [JsonSerializable(typeof(Model))]
    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    private sealed partial class ProtectedPatternsJsonContext : JsonSerializerContext;
}
