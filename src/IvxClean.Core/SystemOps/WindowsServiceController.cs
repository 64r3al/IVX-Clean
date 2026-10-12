using System.ServiceProcess;
using IvxClean.Core.Abstractions;

namespace IvxClean.Core.SystemOps;

/// <summary>Production <see cref="IServiceController"/> backed by the Windows Service Control Manager.</summary>
public sealed class WindowsServiceController : IServiceController
{
    /// <inheritdoc />
    public ServiceRunState GetState(string serviceName)
    {
        try
        {
            using var service = new ServiceController(serviceName);
            return service.Status switch
            {
                ServiceControllerStatus.Running => ServiceRunState.Running,
                ServiceControllerStatus.Stopped => ServiceRunState.Stopped,
                _ => ServiceRunState.Other,
            };
        }
        catch (InvalidOperationException)
        {
            return ServiceRunState.NotInstalled;
        }
    }

    /// <inheritdoc />
    public void StopService(string serviceName, TimeSpan timeout)
    {
        using var service = new ServiceController(serviceName);
        if (service.Status is ServiceControllerStatus.Stopped or ServiceControllerStatus.StopPending)
        {
            return;
        }

        if (service.CanStop)
        {
            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
        }
    }

    /// <inheritdoc />
    public void StartService(string serviceName, TimeSpan timeout)
    {
        using var service = new ServiceController(serviceName);
        if (service.Status is ServiceControllerStatus.Running or ServiceControllerStatus.StartPending)
        {
            return;
        }

        service.Start();
        service.WaitForStatus(ServiceControllerStatus.Running, timeout);
    }
}
