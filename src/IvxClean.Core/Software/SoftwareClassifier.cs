using System.Text.Json;
using System.Text.Json.Serialization;

namespace IvxClean.Core.Software;

/// <summary>
/// Classifies installed programs using the embedded <c>rules/software.json</c> matchers.
/// Precedence: Known bloatware, then Never-list, then Maybe unwanted, then Keep — so a
/// "Driver Updater" is flagged as bloatware rather than hidden by a driver matcher.
/// </summary>
public sealed class SoftwareClassifier
{
    private readonly MatchSet _bloatware;
    private readonly MatchSet _neverList;
    private readonly MatchSet _maybeUnwanted;

    private SoftwareClassifier(Model model)
    {
        _bloatware = MatchSet.From(model.KnownBloatware);
        _neverList = MatchSet.From(model.NeverList);
        _maybeUnwanted = MatchSet.From(model.MaybeUnwanted);
    }

    /// <summary>Loads the classifier from the embedded catalog.</summary>
    public static SoftwareClassifier LoadEmbedded()
    {
        const string resource = "IvxClean.Core.Rules.software.json";
        using Stream stream = typeof(SoftwareClassifier).Assembly.GetManifestResourceStream(resource)
            ?? throw new InvalidOperationException($"Embedded resource '{resource}' was not found.");
        Model model = JsonSerializer.Deserialize(stream, SoftwareJsonContext.Default.Model)
            ?? throw new InvalidOperationException("software.json deserialized to null.");
        return new SoftwareClassifier(model);
    }

    /// <summary>Creates a classifier from explicit matchers (tests).</summary>
    public static SoftwareClassifier FromMatchers(
        (IEnumerable<string> Names, IEnumerable<string> Publishers) bloatware,
        (IEnumerable<string> Names, IEnumerable<string> Publishers) neverList,
        (IEnumerable<string> Names, IEnumerable<string> Publishers) maybeUnwanted)
        => new(new Model
        {
            KnownBloatware = new Matchers { Names = [.. bloatware.Names], Publishers = [.. bloatware.Publishers] },
            NeverList = new Matchers { Names = [.. neverList.Names], Publishers = [.. neverList.Publishers] },
            MaybeUnwanted = new Matchers { Names = [.. maybeUnwanted.Names], Publishers = [.. maybeUnwanted.Publishers] },
        });

    /// <summary>Classifies a single entry.</summary>
    public SoftwareClassification Classify(RawSoftwareEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (_bloatware.Matches(entry))
        {
            return SoftwareClassification.KnownBloatware;
        }

        if (_neverList.Matches(entry))
        {
            return SoftwareClassification.NeverList;
        }

        if (_maybeUnwanted.Matches(entry))
        {
            return SoftwareClassification.MaybeUnwanted;
        }

        return SoftwareClassification.Keep;
    }

    private sealed class MatchSet
    {
        private readonly string[] _names;
        private readonly string[] _publishers;

        private MatchSet(string[] names, string[] publishers)
        {
            _names = names;
            _publishers = publishers;
        }

        public static MatchSet From(Matchers? m)
            => new(m?.Names?.ToArray() ?? [], m?.Publishers?.ToArray() ?? []);

        public bool Matches(RawSoftwareEntry entry)
        {
            foreach (string n in _names)
            {
                if (entry.DisplayName.Contains(n, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            foreach (string p in _publishers)
            {
                if (entry.Publisher.Contains(p, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal sealed class Model
    {
        [JsonPropertyName("knownBloatware")] public Matchers? KnownBloatware { get; set; }

        [JsonPropertyName("neverList")] public Matchers? NeverList { get; set; }

        [JsonPropertyName("maybeUnwanted")] public Matchers? MaybeUnwanted { get; set; }
    }

    internal sealed class Matchers
    {
        [JsonPropertyName("names")] public List<string>? Names { get; set; }

        [JsonPropertyName("publishers")] public List<string>? Publishers { get; set; }
    }
}

/// <summary>Source-generated JSON for the software catalog.</summary>
[JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip)]
[JsonSerializable(typeof(SoftwareClassifier.Model))]
internal sealed partial class SoftwareJsonContext : JsonSerializerContext;
