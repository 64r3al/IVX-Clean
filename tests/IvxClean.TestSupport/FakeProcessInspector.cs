using IvxClean.Core.Abstractions;

namespace IvxClean.TestSupport;

/// <summary>A process inspector that reports a fixed set of running executables.</summary>
public sealed class FakeProcessInspector(params string[] running) : IProcessInspector
{
    private readonly HashSet<string> _running = new(running, StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public IReadOnlySet<string> RunningExecutables() => _running;
}
