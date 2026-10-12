using IvxClean.Core.Rules;
using IvxClean.Core.Scanning;

namespace IvxClean.TestSupport;

/// <summary>Builds <see cref="PlanItem"/> values for cleaner tests from real files.</summary>
public static class PlanItems
{
    /// <summary>Creates an includable file plan item mirroring what the scanner would produce.</summary>
    public static PlanItem ForFile(string path, string baseFolder, RuleAction action = RuleAction.Delete, int minAgeHours = 0, bool allowBaseDeletion = false)
    {
        var info = new FileInfo(path);
        return new PlanItem
        {
            RuleId = "test.rule",
            Category = "Test",
            Risk = action == RuleAction.Quarantine ? RuleRisk.Caution : RuleRisk.Safe,
            Action = action,
            Path = path,
            BaseFolder = baseFolder,
            MinAgeHours = minAgeHours,
            AllowBaseDeletion = allowBaseDeletion,
            IsDirectory = false,
            Size = info.Exists ? info.Length : 0,
            LastWriteUtc = info.Exists ? info.LastWriteTimeUtc : DateTime.MinValue,
            Decision = PlanDecision.Include,
            Reason = "Eligible to clean.",
        };
    }
}
