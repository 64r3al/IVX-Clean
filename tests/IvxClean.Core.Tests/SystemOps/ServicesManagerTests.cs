using IvxClean.Core.SystemOps;
using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.SystemOps;

public sealed class ServicesManagerTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();

    public void Dispose() => _roots.Dispose();

    [Fact]
    public async Task Running_services_are_stopped_during_the_action_and_restarted_after()
    {
        var controller = new FakeServiceController().With("wuauserv", true).With("bits", true).With("dosvc", false);
        var manager = new ServicesManager(controller, _roots);
        bool wasStoppedDuringAction = false;

        await manager.RunWithServicesStoppedAsync(["wuauserv", "bits", "dosvc"], _ =>
        {
            wasStoppedDuringAction = !controller.IsRunning("wuauserv") && !controller.IsRunning("bits");
            return Task.CompletedTask;
        }, CancellationToken.None);

        wasStoppedDuringAction.ShouldBeTrue();
        controller.IsRunning("wuauserv").ShouldBeTrue("originally-running services are restored");
        controller.IsRunning("bits").ShouldBeTrue();
        controller.IsRunning("dosvc").ShouldBeFalse("originally-stopped services stay stopped");
    }

    [Fact]
    public async Task Original_states_are_restored_even_when_the_action_throws()
    {
        var controller = new FakeServiceController().With("wuauserv", true);
        var manager = new ServicesManager(controller, _roots);

        await Should.ThrowAsync<InvalidOperationException>(() =>
            manager.RunWithServicesStoppedAsync(["wuauserv"], _ => throw new InvalidOperationException("boom"), CancellationToken.None));

        controller.IsRunning("wuauserv").ShouldBeTrue();
    }

    [Fact]
    public async Task Original_states_are_restored_on_cancellation()
    {
        var controller = new FakeServiceController().With("wuauserv", true);
        var manager = new ServicesManager(controller, _roots);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            manager.RunWithServicesStoppedAsync(["wuauserv"], _ => Task.CompletedTask, cts.Token));

        controller.IsRunning("wuauserv").ShouldBeTrue();
    }

    [Fact]
    public async Task A_crash_marker_restarts_services_on_next_start()
    {
        // First manager "crashes": simulate by stopping via the action and never letting finally run.
        var controller = new FakeServiceController().With("wuauserv", true).With("bits", true);
        var crashing = new ServicesManager(controller, _roots);

        // The crash marker is written before stopping; emulate a crash by stopping the services and
        // leaving the marker in place (the action throws through a path that still writes the marker).
        try
        {
            await crashing.RunWithServicesStoppedAsync(["wuauserv", "bits"], _ =>
            {
                // Simulate process death mid-action: force both stopped and abort hard.
                throw new OperationCanceledException();
            }, CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            // finally restored them; emulate a real crash by stopping again and re-writing a marker
            // through a fresh manager that recovers.
        }

        // Now simulate the services left stopped by a true crash, with a marker present.
        controller.StopService("wuauserv", TimeSpan.Zero);
        controller.StopService("bits", TimeSpan.Zero);
        WriteCrashMarker("wuauserv", "bits");

        var recovering = new ServicesManager(controller, _roots);
        recovering.RecoverIfCrashed();

        controller.IsRunning("wuauserv").ShouldBeTrue();
        controller.IsRunning("bits").ShouldBeTrue();
    }

    private void WriteCrashMarker(params string[] services)
    {
        var marker = new ServiceRecoveryMarker();
        foreach (string s in services)
        {
            marker.Services.Add(new ServiceState { Name = s, WasRunning = true });
        }

        string path = Path.Combine(_roots.AppDataRoot, "services-recovery.json");
        File.WriteAllText(path, System.Text.Json.JsonSerializer.Serialize(marker, ServicesJsonContext.Default.ServiceRecoveryMarker));
    }
}

public sealed class RestorePointDecisionTests
{
    [Fact]
    public void Created_when_count_increases()
        => WmiRestorePointService.DecideOutcome(0, 3, 4).ShouldBe(RestorePointOutcome.Created);

    [Fact]
    public void Frequency_limited_when_count_unchanged()
        => WmiRestorePointService.DecideOutcome(0, 3, 3).ShouldBe(RestorePointOutcome.SkippedByFrequencyLimit);

    [Fact]
    public void Protection_disabled_is_reported()
        => WmiRestorePointService.DecideOutcome(1058, 0, 0).ShouldBe(RestorePointOutcome.ProtectionDisabled);

    [Fact]
    public void Other_failures_are_reported()
        => WmiRestorePointService.DecideOutcome(5, 0, 0).ShouldBe(RestorePointOutcome.Failed);
}
