using System.Text.Json;

namespace IvxClean.Core.Rules;

/// <summary>
/// The in-memory set of all cleaning rules, loaded from the JSON catalog embedded into Core.
/// </summary>
public sealed class RuleCatalog
{
    private const string ResourcePrefix = "IvxClean.Core.Catalog.";

    private RuleCatalog(IReadOnlyList<Rule> rules) => Rules = rules;

    /// <summary>All rules, in catalog order.</summary>
    public IReadOnlyList<Rule> Rules { get; }

    /// <summary>Loads and validates the catalog embedded in this assembly.</summary>
    public static RuleCatalog LoadEmbedded()
    {
        System.Reflection.Assembly assembly = typeof(RuleCatalog).Assembly;
        var rules = new List<Rule>();

        foreach (string name in assembly.GetManifestResourceNames())
        {
            if (!name.StartsWith(ResourcePrefix, StringComparison.Ordinal) ||
                !name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            using Stream stream = assembly.GetManifestResourceStream(name)
                ?? throw new InvalidOperationException($"Catalog resource '{name}' could not be opened.");
            RuleCatalogFile? file = JsonSerializer.Deserialize(stream, RuleJsonContext.Default.RuleCatalogFile);
            if (file is not null)
            {
                rules.AddRange(file.Rules);
            }
        }

        return Create(rules);
    }

    /// <summary>Creates a catalog from an explicit rule set, enforcing unique ids (used by tests).</summary>
    public static RuleCatalog Create(IReadOnlyList<Rule> rules)
    {
        ArgumentNullException.ThrowIfNull(rules);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (Rule rule in rules)
        {
            if (!seen.Add(rule.Id))
            {
                throw new InvalidOperationException($"Duplicate rule id in catalog: '{rule.Id}'.");
            }
        }

        return new RuleCatalog([.. rules.OrderBy(r => r.Level).ThenBy(r => r.Category, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal)]);
    }

    /// <summary>Returns the rules that apply at or below the given level and match the given risk filter.</summary>
    public IEnumerable<Rule> ForLevel(int maxLevel, bool includeExpert)
        => Rules.Where(r => r.Level <= maxLevel && (includeExpert || r.Risk != RuleRisk.Expert));
}
