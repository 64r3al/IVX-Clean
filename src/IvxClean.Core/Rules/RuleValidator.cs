using System.Text.RegularExpressions;

namespace IvxClean.Core.Rules;

/// <summary>
/// Validates rules against the catalog contract (mirrors <c>rules/schema.json</c>). The rules
/// tests fail on any error, so an invalid or unsafe rule can never ship.
/// </summary>
public static partial class RuleValidator
{
    [GeneratedRegex("^[a-z0-9]+(?:[.-][a-z0-9]+)*$")]
    private static partial Regex IdPattern();

    private static readonly string[] KnownTokens =
    [
        "{Temp}", "{LocalAppData}", "{RoamingAppData}", "{LocalLowAppData}", "{ProgramData}",
        "{Windows}", "{SystemDrive}", "{UserProfile}", "{EachUserProfile}", "{EachFixedDrive}",
    ];

    /// <summary>Returns every validation error found in the catalog (empty when valid).</summary>
    public static IReadOnlyList<string> Validate(RuleCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);

        var errors = new List<string>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Rule rule in catalog.Rules)
        {
            string where = string.IsNullOrWhiteSpace(rule.Id) ? "<missing id>" : rule.Id;

            if (string.IsNullOrWhiteSpace(rule.Id) || !IdPattern().IsMatch(rule.Id))
            {
                errors.Add($"{where}: id must be lower-case dotted/kebab (e.g. 'browser.chrome.cache').");
            }
            else if (!ids.Add(rule.Id))
            {
                errors.Add($"{where}: duplicate id.");
            }

            if (string.IsNullOrWhiteSpace(rule.Name))
            {
                errors.Add($"{where}: name is required.");
            }

            if (string.IsNullOrWhiteSpace(rule.Category))
            {
                errors.Add($"{where}: category is required.");
            }

            if (rule.Level is < 1 or > 4)
            {
                errors.Add($"{where}: level must be 1-4 (was {rule.Level}).");
            }

            if (string.IsNullOrWhiteSpace(rule.Why))
            {
                errors.Add($"{where}: 'why' is required.");
            }

            if (rule.Sources.Count == 0 || rule.Sources.Any(string.IsNullOrWhiteSpace))
            {
                errors.Add($"{where}: at least one non-empty source is required.");
            }

            if (rule.Risk == RuleRisk.Expert && rule.Level != 4)
            {
                errors.Add($"{where}: expert rules must be level 4.");
            }

            ValidateActionAndTargets(rule, where, errors);
        }

        return errors;
    }

    private static void ValidateActionAndTargets(Rule rule, string where, List<string> errors)
    {
        if (rule.Action == RuleAction.Command)
        {
            if (string.IsNullOrWhiteSpace(rule.Command))
            {
                errors.Add($"{where}: command action requires a 'command' name.");
            }

            if (rule.Targets.Count != 0)
            {
                errors.Add($"{where}: command action must not declare file targets.");
            }

            return;
        }

        if (!string.IsNullOrWhiteSpace(rule.Command))
        {
            errors.Add($"{where}: 'command' is only valid with the command action.");
        }

        if (rule.Targets.Count == 0)
        {
            errors.Add($"{where}: delete/quarantine rules must declare at least one target.");
        }

        foreach (RuleTarget target in rule.Targets)
        {
            if (string.IsNullOrWhiteSpace(target.Base) || target.Base[0] != '{')
            {
                errors.Add($"{where}: target base must start with a path token (was '{target.Base}').");
                continue;
            }

            string token = target.Base[..(target.Base.IndexOf('}') is var i && i > 0 ? i + 1 : target.Base.Length)];
            if (!KnownTokens.Contains(token))
            {
                errors.Add($"{where}: unknown path token '{token}' in target base.");
            }

            if (target.MinAgeHours < 0)
            {
                errors.Add($"{where}: minAgeHours must be >= 0.");
            }

            foreach (string sub in target.SubPaths)
            {
                if (sub.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(sub))
                {
                    errors.Add($"{where}: subPath '{sub}' must be relative and must not contain '..'.");
                }
            }
        }
    }
}
