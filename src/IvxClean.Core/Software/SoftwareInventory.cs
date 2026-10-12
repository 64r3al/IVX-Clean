namespace IvxClean.Core.Software;

/// <summary>Supplies the raw installed-software entries (registry + AppX).</summary>
public interface IInstalledSoftwareSource
{
    /// <summary>Returns every installed program this source knows about, unclassified.</summary>
    IReadOnlyList<RawSoftwareEntry> GetRaw();
}

/// <summary>The three visible groups of installed software (Never-list is hidden).</summary>
public sealed record SoftwareInventoryResult(
    IReadOnlyList<SoftwareEntry> KnownBloatware,
    IReadOnlyList<SoftwareEntry> MaybeUnwanted,
    IReadOnlyList<SoftwareEntry> Keep);

/// <summary>Builds the classified inventory, hiding never-listed programs entirely.</summary>
public sealed class SoftwareInventory
{
    private readonly IEnumerable<IInstalledSoftwareSource> _sources;
    private readonly SoftwareClassifier _classifier;

    /// <summary>Creates the inventory over the given sources and classifier.</summary>
    public SoftwareInventory(IEnumerable<IInstalledSoftwareSource> sources, SoftwareClassifier classifier)
    {
        _sources = sources ?? throw new ArgumentNullException(nameof(sources));
        _classifier = classifier ?? throw new ArgumentNullException(nameof(classifier));
    }

    /// <summary>Reads, de-duplicates and classifies the inventory. Never-listed items are omitted.</summary>
    public SoftwareInventoryResult Build()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bloat = new List<SoftwareEntry>();
        var maybe = new List<SoftwareEntry>();
        var keep = new List<SoftwareEntry>();

        foreach (IInstalledSoftwareSource source in _sources)
        {
            foreach (RawSoftwareEntry raw in source.GetRaw())
            {
                if (!seen.Add($"{raw.DisplayName}|{raw.Version}|{raw.Source}"))
                {
                    continue;
                }

                SoftwareClassification classification = _classifier.Classify(raw);
                if (classification == SoftwareClassification.NeverList)
                {
                    continue; // hidden entirely — can never be uninstalled from the app
                }

                var entry = new SoftwareEntry { Raw = raw, Classification = classification };
                switch (classification)
                {
                    case SoftwareClassification.KnownBloatware:
                        bloat.Add(entry);
                        break;
                    case SoftwareClassification.MaybeUnwanted:
                        maybe.Add(entry);
                        break;
                    default:
                        keep.Add(entry);
                        break;
                }
            }
        }

        static List<SoftwareEntry> Sorted(List<SoftwareEntry> list)
            => [.. list.OrderBy(e => e.DisplayName, StringComparer.CurrentCultureIgnoreCase)];

        return new SoftwareInventoryResult(Sorted(bloat), Sorted(maybe), Sorted(keep));
    }
}
