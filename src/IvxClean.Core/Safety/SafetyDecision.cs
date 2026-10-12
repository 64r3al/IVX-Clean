namespace IvxClean.Core.Safety;

/// <summary>
/// The result of a SafetyGuard evaluation: either allowed, or denied with a reason and a
/// short human-readable explanation suitable for the UI.
/// </summary>
public readonly record struct SafetyDecision
{
    private SafetyDecision(bool allowed, DenyReason reason, string explanation)
    {
        IsAllowed = allowed;
        Reason = reason;
        Explanation = explanation;
    }

    /// <summary>Whether the path may be acted upon.</summary>
    public bool IsAllowed { get; }

    /// <summary>Whether the path was denied (the complement of <see cref="IsAllowed"/>).</summary>
    public bool IsDenied => !IsAllowed;

    /// <summary>The denial reason, or <see cref="DenyReason.None"/> when allowed.</summary>
    public DenyReason Reason { get; }

    /// <summary>A short, user-facing explanation of the decision.</summary>
    public string Explanation { get; }

    /// <summary>The singleton allowed decision.</summary>
    public static SafetyDecision Allowed { get; } = new(true, DenyReason.None, "Allowed.");

    /// <summary>Creates a denial decision with the given reason and explanation.</summary>
    public static SafetyDecision Deny(DenyReason reason, string explanation) => new(false, reason, explanation);
}
