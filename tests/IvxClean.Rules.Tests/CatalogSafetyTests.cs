using IvxClean.Core.Rules;
using IvxClean.Core.Safety;
using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Rules.Tests;

/// <summary>
/// Proves that no rule in the catalog points at a protected location: every resolved target,
/// mapped into a sandbox, must allow a benign file through the SafetyGuard. A rule aimed at
/// Documents, a system folder, Program Files, etc. fails this test.
/// </summary>
public sealed class CatalogSafetyTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();
    private static readonly RuleCatalog Catalog = RuleCatalog.LoadEmbedded();

    public void Dispose() => _roots.Dispose();

    [Fact]
    public void No_rule_target_resolves_to_a_protected_location()
    {
        SafetyGuard guard = BuildGuard();
        var failures = new List<string>();
        var probeTime = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        foreach (Rule rule in Catalog.Rules)
        {
            foreach (RuleTarget target in rule.Targets)
            {
                foreach (string cleanRoot in ResolveCleanRoots(target))
                {
                    Directory.CreateDirectory(cleanRoot);
                    string probe = Path.Combine(cleanRoot, "ivx-probe.bin");
                    File.WriteAllText(probe, "probe");
                    File.SetLastWriteTimeUtc(probe, probeTime);

                    SafetyDecision decision = guard.Evaluate(probe, new SafetyTarget(cleanRoot, target.MinAgeHours, target.DeleteBaseIfEmpty));
                    if (decision.IsDenied)
                    {
                        failures.Add($"{rule.Id} -> {cleanRoot}: {decision.Reason} ({decision.Explanation})");
                    }
                }
            }
        }

        failures.ShouldBeEmpty();
    }

    private SafetyGuard BuildGuard()
    {
        // Simulate the machine's protected user folders inside the sandbox, so a rule that aimed
        // at a personal folder would be caught here.
        var protectedUser = new List<string>();
        foreach (string profile in new[] { _roots.UserProfile }.Concat(_roots.EachUserProfile).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            foreach (string folder in new[] { "Documents", "Downloads", "Desktop", "Pictures", "Videos", "Music", "Saved Games", "OneDrive" })
            {
                protectedUser.Add(Path.Combine(profile, folder));
            }
        }

        SafetyPolicy policy = new(ProtectedPatterns.LoadEmbedded(), protectedUser, []);
        return new SafetyGuard(_roots, FakeClock.AtDefault(), policy);
    }

    private IEnumerable<string> ResolveCleanRoots(RuleTarget target)
    {
        foreach (string expandedBase in _roots.Expand(target.Base))
        {
            // Mimic the scanner's per-profile expansion with a synthetic profile folder.
            IEnumerable<string> effectiveBases;
            if (target.ProfileDirs is { } spec)
            {
                string profile = Path.Combine(expandedBase, "TestProfile");
                Directory.CreateDirectory(profile);
                if (!string.IsNullOrEmpty(spec.RequireFile))
                {
                    File.WriteAllText(Path.Combine(profile, spec.RequireFile), "{}");
                }

                effectiveBases = [profile];
            }
            else
            {
                effectiveBases = [expandedBase];
            }

            foreach (string eb in effectiveBases)
            {
                if (target.SubPaths.Count == 0)
                {
                    yield return eb;
                }
                else
                {
                    foreach (string sub in target.SubPaths)
                    {
                        yield return Path.Combine(eb, sub);
                    }
                }
            }
        }
    }
}
