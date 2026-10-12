using System.Globalization;
using System.Text;
using IvxClean.Core.Rules;

// Generates docs/RULES.md from the embedded rule catalog. CI runs this and fails if the
// committed file is out of date (git diff --exit-code).

string output = args.Length > 0 ? args[0] : Path.Combine("docs", "RULES.md");
RuleCatalog catalog = RuleCatalog.LoadEmbedded();
string markdown = RulesMarkdown.Render(catalog);

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
File.WriteAllText(output, markdown);
Console.WriteLine($"Wrote {catalog.Rules.Count} rules to {output}.");
return 0;

/// <summary>Renders the rule catalog as Markdown.</summary>
internal static class RulesMarkdown
{
    private static readonly string[] LevelNames = ["", "Quick", "Standard", "Deep", "Expert"];

    public static string Render(RuleCatalog catalog)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# IVX Clean — Cleaning rules");
        sb.AppendLine();
        sb.AppendLine("> This file is generated from the rule catalog by `tools/RulesDocGen`. Do not edit by hand.");
        sb.AppendLine();
        sb.AppendLine("IVX Clean cleans **only** the locations listed below. Every rule is an explicit, reviewed");
        sb.AppendLine("entry; if a location is not covered by a rule, the cleaner cannot touch it. Each item below");
        sb.AppendLine("also passes the SafetyGuard, which refuses protected system and personal locations, reparse");
        sb.AppendLine("points, and protected file types — even when a rule would otherwise match.");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"**{catalog.Rules.Count} rules** across four levels.");
        sb.AppendLine();

        foreach (int level in new[] { 1, 2, 3, 4 })
        {
            List<Rule> rules = catalog.Rules.Where(r => r.Level == level).OrderBy(r => r.Category, StringComparer.Ordinal).ThenBy(r => r.Id, StringComparer.Ordinal).ToList();
            if (rules.Count == 0)
            {
                continue;
            }

            sb.AppendLine(CultureInfo.InvariantCulture, $"## Level {level} — {LevelNames[level]}");
            sb.AppendLine();
            foreach (Rule rule in rules)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"### {rule.Name}  `{rule.Id}`");
                sb.AppendLine();
                sb.AppendLine(CultureInfo.InvariantCulture, $"- **Category:** {rule.Category}");
                sb.AppendLine(CultureInfo.InvariantCulture, $"- **Risk:** {rule.Risk} · **Action:** {rule.Action}");
                if (rule.RequiresClosed.Count > 0)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Close first:** {string.Join(", ", rule.RequiresClosed)}");
                }

                sb.AppendLine(CultureInfo.InvariantCulture, $"- **Why:** {rule.Why}");
                if (rule.Targets.Count > 0)
                {
                    sb.AppendLine("- **Locations:**");
                    foreach (RuleTarget target in rule.Targets)
                    {
                        string subs = target.SubPaths.Count > 0 ? " → " + string.Join(", ", target.SubPaths) : string.Empty;
                        sb.AppendLine(CultureInfo.InvariantCulture, $"  - `{target.Base}`{subs}");
                    }
                }

                if (rule.Command is not null)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"- **Operation:** `{rule.Command}`");
                }

                sb.AppendLine();
            }
        }

        return sb.ToString().ReplaceLineEndings("\n");
    }
}
