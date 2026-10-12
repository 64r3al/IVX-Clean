namespace IvxClean.Core.Abstractions;

/// <summary>The running state of a Windows service, as far as IVX Clean cares.</summary>
public enum ServiceRunState
{
    /// <summary>The service does not exist on this machine.</summary>
    NotInstalled,

    /// <summary>The service is running.</summary>
    Running,

    /// <summary>The service is stopped.</summary>
    Stopped,

    /// <summary>The service is in a transitional or unknown state.</summary>
    Other,
}

/// <summary>
/// Abstracts the Windows service controller so the services manager can be tested with a fake.
/// IVX Clean only ever stops and restarts a small, named set of update-related services.
/// </summary>
public interface IServiceController
{
    /// <summary>Returns the current state of the named service.</summary>
    ServiceRunState GetState(string serviceName);

    /// <summary>Stops the service and waits up to <paramref name="timeout"/>. No-op if already stopped.</summary>
    void StopService(string serviceName, TimeSpan timeout);

    /// <summary>Starts the service and waits up to <paramref name="timeout"/>. No-op if already running.</summary>
    void StartService(string serviceName, TimeSpan timeout);
}
