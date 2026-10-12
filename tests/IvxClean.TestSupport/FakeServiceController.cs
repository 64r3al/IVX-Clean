using IvxClean.Core.Abstractions;

namespace IvxClean.TestSupport;

/// <summary>An in-memory service controller for testing the services manager.</summary>
public sealed class FakeServiceController : IServiceController
{
    private readonly Dictionary<string, bool> _running = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Service names whose <see cref="Start"/> should throw, to simulate a stuck service.</summary>
    public HashSet<string> FailToStart { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Registers a service with an initial running state.</summary>
    public FakeServiceController With(string name, bool running)
    {
        _running[name] = running;
        return this;
    }

    /// <summary>Whether a registered service is currently running.</summary>
    public bool IsRunning(string name) => _running.TryGetValue(name, out bool r) && r;

    /// <inheritdoc />
    public ServiceRunState GetState(string serviceName)
        => _running.TryGetValue(serviceName, out bool running)
            ? running ? ServiceRunState.Running : ServiceRunState.Stopped
            : ServiceRunState.NotInstalled;

    /// <inheritdoc />
    public void StopService(string serviceName, TimeSpan timeout)
    {
        if (_running.ContainsKey(serviceName))
        {
            _running[serviceName] = false;
        }
    }

    /// <inheritdoc />
    public void StartService(string serviceName, TimeSpan timeout)
    {
        if (FailToStart.Contains(serviceName))
        {
            throw new InvalidOperationException($"Simulated failure starting {serviceName}.");
        }

        if (_running.ContainsKey(serviceName))
        {
            _running[serviceName] = true;
        }
    }
}
