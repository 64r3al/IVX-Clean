namespace IvxClean.Core.Abstractions;

/// <summary>
/// Abstracts "now" so age-based safety checks are deterministic under test.
/// </summary>
public interface IClock
{
    /// <summary>The current UTC time.</summary>
    DateTime UtcNow { get; }
}

/// <summary>The production clock, backed by the system wall clock.</summary>
public sealed class SystemClock : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}
