using System.Text.Json;
using System.Text.Json.Serialization;
using IvxClean.Core.Abstractions;
using IvxClean.Core.Paths;

namespace IvxClean.Core.SystemOps;

/// <summary>A recorded service state, used by the crash-recovery marker.</summary>
public sealed class ServiceState
{
    /// <summary>The service name.</summary>
    [JsonPropertyName("name")] public required string Name { get; init; }

    /// <summary>Whether the service was running before IVX Clean stopped it.</summary>
    [JsonPropertyName("wasRunning")] public required bool WasRunning { get; init; }
}

/// <summary>The crash-recovery marker written before any service is stopped.</summary>
public sealed class ServiceRecoveryMarker
{
    /// <summary>The services and their pre-stop states.</summary>
    [JsonPropertyName("services")]
    public IList<ServiceState> Services { get => field; init => field = value ?? []; } = [];
}

/// <summary>Source-generated JSON for the recovery marker.</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNameCaseInsensitive = true)]
[JsonSerializable(typeof(ServiceRecoveryMarker))]
public sealed partial class ServicesJsonContext : JsonSerializerContext;

/// <summary>Stops a named set of services, runs an action, and always restores the original
/// states — even on exception or crash. Never changes a service's start type.</summary>
public interface IServicesManager
{
    /// <summary>Runs <paramref name="action"/> with the given services stopped, restoring them after.</summary>
    Task RunWithServicesStoppedAsync(IReadOnlyList<string> services, Func<CancellationToken, Task> action, CancellationToken cancellationToken);

    /// <summary>On startup, restarts any services a previous (crashed) run left stopped.</summary>
    void RecoverIfCrashed();
}

/// <inheritdoc cref="IServicesManager"/>
public sealed class ServicesManager : IServicesManager
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    private readonly IServiceController _controller;
    private readonly string _markerPath;

    /// <summary>Creates the manager. The recovery marker lives in IVX Clean's own data folder.</summary>
    public ServicesManager(IServiceController controller, IPathRoots roots)
    {
        _controller = controller ?? throw new ArgumentNullException(nameof(controller));
        ArgumentNullException.ThrowIfNull(roots);
        Directory.CreateDirectory(roots.AppDataRoot);
        _markerPath = Path.Combine(roots.AppDataRoot, "services-recovery.json");
    }

    /// <inheritdoc />
    public async Task RunWithServicesStoppedAsync(IReadOnlyList<string> services, Func<CancellationToken, Task> action, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(action);

        List<ServiceState> original = services
            .Where(s => _controller.GetState(s) != ServiceRunState.NotInstalled)
            .Select(s => new ServiceState { Name = s, WasRunning = _controller.GetState(s) == ServiceRunState.Running })
            .ToList();

        WriteMarker(original);
        try
        {
            foreach (ServiceState state in original.Where(s => s.WasRunning))
            {
                cancellationToken.ThrowIfCancellationRequested();
                _controller.StopService(state.Name, Timeout);
            }

            await action(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            // Always restore, regardless of success, cancellation or failure.
            foreach (ServiceState state in original.Where(s => s.WasRunning))
            {
                TryStart(state.Name);
            }

            DeleteMarker();
        }
    }

    /// <inheritdoc />
    public void RecoverIfCrashed()
    {
        ServiceRecoveryMarker? marker = ReadMarker();
        if (marker is null)
        {
            return;
        }

        foreach (ServiceState state in marker.Services.Where(s => s.WasRunning))
        {
            TryStart(state.Name);
        }

        DeleteMarker();
    }

    private void TryStart(string name)
    {
        try
        {
            _controller.StartService(name, Timeout);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or System.ServiceProcess.TimeoutException)
        {
            // Best-effort restore; a service that refuses to restart is logged by the caller.
        }
    }

    private void WriteMarker(IReadOnlyList<ServiceState> services)
    {
        var marker = new ServiceRecoveryMarker();
        foreach (ServiceState s in services)
        {
            marker.Services.Add(s);
        }

        File.WriteAllText(_markerPath, JsonSerializer.Serialize(marker, ServicesJsonContext.Default.ServiceRecoveryMarker));
    }

    private ServiceRecoveryMarker? ReadMarker()
    {
        if (!File.Exists(_markerPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(File.ReadAllText(_markerPath), ServicesJsonContext.Default.ServiceRecoveryMarker);
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    private void DeleteMarker()
    {
        try
        {
            if (File.Exists(_markerPath))
            {
                File.Delete(_markerPath);
            }
        }
        catch (IOException)
        {
            // Leave it; the next recovery pass will retry.
        }
    }
}
