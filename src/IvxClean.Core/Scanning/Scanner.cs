using System.IO.Enumeration;
using IvxClean.Core.Abstractions;
using IvxClean.Core.IO;
using IvxClean.Core.Paths;
using IvxClean.Core.Rules;
using IvxClean.Core.Safety;

namespace IvxClean.Core.Scanning;

/// <summary>
/// The read-only scanner. It evaluates each rule's detection, expands and walks its targets
/// (never following reparse points), passes every candidate through the <see cref="ISafetyGuard"/>,
/// and records the decision. It never deletes anything.
/// </summary>
public sealed class Scanner : IScanner
{
    private readonly RuleCatalog _catalog;
    private readonly IPathRoots _roots;
    private readonly ISafetyGuard _guard;
    private readonly IProcessInspector _processes;

    /// <summary>Creates a scanner over the given catalog, roots, guard and process inspector.</summary>
    public Scanner(RuleCatalog catalog, IPathRoots roots, ISafetyGuard guard, IProcessInspector processes)
    {
        _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
        _roots = roots ?? throw new ArgumentNullException(nameof(roots));
        _guard = guard ?? throw new ArgumentNullException(nameof(guard));
        _processes = processes ?? throw new ArgumentNullException(nameof(processes));
    }

    /// <inheritdoc />
    public Task<CleanPlan> ScanAsync(int level, bool includeExpert, IProgress<ScanProgress>? progress, CancellationToken cancellationToken)
    {
        List<Rule> rules = _catalog.ForLevel(level, includeExpert).ToList();
        IReadOnlySet<string> running = _processes.RunningExecutables();

        return Task.Run(() =>
        {
            var items = new List<PlanItem>();
            long bytesFound = 0;
            long examined = 0;
            int completed = 0;

            foreach (Rule rule in rules)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!DetectApplies(rule))
                {
                    Report(progress, rule.Category, string.Empty, bytesFound, examined, completed, rules.Count);
                    completed++;
                    continue;
                }

                string? lockingProcess = rule.RequiresClosed.FirstOrDefault(running.Contains);

                foreach (RuleTarget target in rule.Targets)
                {
                    foreach (string cleanRoot in ResolveCleanRoots(target))
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        ScanCleanRoot(rule, target, cleanRoot, lockingProcess, items, ref bytesFound, ref examined, progress, completed, rules.Count, cancellationToken);
                    }
                }

                completed++;
                Report(progress, rule.Category, string.Empty, bytesFound, examined, completed, rules.Count);
            }

            return new CleanPlan(level, includeExpert, items);
        }, cancellationToken);
    }

    private IEnumerable<string> ResolveCleanRoots(RuleTarget target)
    {
        foreach (string expandedBase in _roots.Expand(target.Base))
        {
            foreach (string effectiveBase in ResolveEffectiveBases(expandedBase, target.ProfileDirs))
            {
                if (target.SubPaths.Count == 0)
                {
                    yield return effectiveBase;
                }
                else
                {
                    foreach (string sub in target.SubPaths)
                    {
                        yield return Path.Combine(effectiveBase, sub);
                    }
                }
            }
        }
    }

    private static IEnumerable<string> ResolveEffectiveBases(string baseFolder, ProfileDirSpec? profileDirs)
    {
        if (profileDirs is null)
        {
            yield return baseFolder;
            yield break;
        }

        if (!Directory.Exists(baseFolder))
        {
            yield break;
        }

        IEnumerable<string> subdirs;
        try
        {
            subdirs = Directory.EnumerateDirectories(baseFolder);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or DirectoryNotFoundException)
        {
            yield break;
        }

        foreach (string dir in subdirs)
        {
            if (string.IsNullOrEmpty(profileDirs.RequireFile) || File.Exists(Path.Combine(dir, profileDirs.RequireFile)))
            {
                yield return dir;
            }
        }
    }

    private void ScanCleanRoot(
        Rule rule, RuleTarget target, string cleanRoot, string? lockingProcess,
        List<PlanItem> items, ref long bytesFound, ref long examined,
        IProgress<ScanProgress>? progress, int completed, int totalRules, CancellationToken ct)
    {
        if (!Directory.Exists(cleanRoot))
        {
            return;
        }

        bool restrictToFiles = target.FileGlobs.Count > 0;
        var safetyTarget = new SafetyTarget(cleanRoot, target.MinAgeHours, target.DeleteBaseIfEmpty);

        foreach (FsEntry entry in SafeFileEnumerator.Enumerate(cleanRoot, recursive: true, includeDirectories: !restrictToFiles))
        {
            ct.ThrowIfCancellationRequested();
            examined++;

            if (restrictToFiles && !MatchesAnyGlob(Path.GetFileName(entry.Path), target.FileGlobs))
            {
                continue;
            }

            SafetyDecision decision = _guard.Evaluate(entry.Path, safetyTarget);

            long size = 0;
            DateTime lastWrite = DateTime.MinValue;
            if (!entry.IsDirectory)
            {
                try
                {
                    var info = new FileInfo(entry.Path);
                    size = info.Length;
                    lastWrite = info.LastWriteTimeUtc;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    continue;
                }
            }

            PlanDecision planDecision;
            string reason;
            if (decision.IsDenied)
            {
                planDecision = ClassifyDenial(decision.Reason);
                reason = decision.Explanation;
            }
            else if (lockingProcess is not null)
            {
                planDecision = PlanDecision.Skipped;
                reason = $"Close {lockingProcess} to include this.";
            }
            else
            {
                planDecision = PlanDecision.Include;
                reason = "Eligible to clean.";
                bytesFound += size;
            }

            items.Add(new PlanItem
            {
                RuleId = rule.Id,
                Category = rule.Category,
                Risk = rule.Risk,
                Action = rule.Action,
                Path = entry.Path,
                BaseFolder = cleanRoot,
                MinAgeHours = target.MinAgeHours,
                AllowBaseDeletion = target.DeleteBaseIfEmpty,
                IsDirectory = entry.IsDirectory,
                Size = size,
                LastWriteUtc = lastWrite,
                Decision = planDecision,
                Reason = reason,
            });

            if ((examined & 0x3FF) == 0)
            {
                Report(progress, rule.Category, entry.Path, bytesFound, examined, completed, totalRules);
            }
        }
    }

    private bool DetectApplies(Rule rule)
    {
        if (rule.Detect is null || rule.Detect.AnyExists.Count == 0)
        {
            return true;
        }

        foreach (string token in rule.Detect.AnyExists)
        {
            foreach (string path in _roots.Expand(token))
            {
                if (Directory.Exists(path) || File.Exists(path))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool MatchesAnyGlob(string name, IReadOnlyList<string> globs)
    {
        foreach (string glob in globs)
        {
            if (FileSystemName.MatchesSimpleExpression(glob, name, ignoreCase: true))
            {
                return true;
            }
        }

        return false;
    }

    private static PlanDecision ClassifyDenial(DenyReason reason) => reason switch
    {
        DenyReason.TooRecent or DenyReason.Unresolvable => PlanDecision.Skipped,
        _ => PlanDecision.Protected,
    };

    private static void Report(IProgress<ScanProgress>? progress, string category, string path, long bytes, long examined, int completed, int total)
        => progress?.Report(new ScanProgress
        {
            Category = category,
            CurrentPath = path,
            BytesFound = bytes,
            ItemsExamined = examined,
            RulesCompleted = completed,
            RulesTotal = total,
        });
}
