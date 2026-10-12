namespace IvxClean.Core.Safety;

/// <summary>
/// The minimal information the <see cref="SafetyGuard"/> needs about the rule target a candidate
/// path belongs to. The full rule model (added with the rules engine) adapts onto this.
/// </summary>
public interface ISafetyTarget
{
    /// <summary>The target's resolved base folder (absolute; a single expanded path).</summary>
    string ResolvedBase { get; }

    /// <summary>Minimum file age in hours; a file modified more recently is skipped. 0 disables.</summary>
    int MinAgeHours { get; }

    /// <summary>
    /// Whether the base folder itself may be deleted when empty (the rule's <c>deleteBaseIfEmpty</c>).
    /// </summary>
    bool AllowBaseDeletion { get; }
}

/// <summary>A plain <see cref="ISafetyTarget"/> value for rules and tests.</summary>
public sealed record SafetyTarget(string ResolvedBase, int MinAgeHours = 0, bool AllowBaseDeletion = false)
    : ISafetyTarget;
