using IvxClean.Core.Abstractions;

namespace IvxClean.TestSupport;

/// <summary>A controllable clock for deterministic age-based tests.</summary>
public sealed class FakeClock(DateTime utcNow) : IClock
{
    /// <inheritdoc />
    public DateTime UtcNow { get; set; } = utcNow;

    /// <summary>A clock fixed at a fixed, arbitrary instant.</summary>
    public static FakeClock AtDefault() => new(new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc));
}
