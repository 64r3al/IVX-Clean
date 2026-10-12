namespace IvxClean.Core.Safety;

/// <summary>
/// The single veto authority over every path IVX Clean may delete or quarantine. It is called
/// at scan time and again immediately before each delete/quarantine, to defend against
/// time-of-check/time-of-use changes. When in doubt it denies.
/// </summary>
public interface ISafetyGuard
{
    /// <summary>
    /// Evaluates whether <paramref name="candidatePath"/>, belonging to <paramref name="target"/>,
    /// is safe to act upon. Never throws for an ordinary filesystem condition; any ambiguity,
    /// error or unexpected state results in a denial.
    /// </summary>
    SafetyDecision Evaluate(string candidatePath, ISafetyTarget target);
}
