using IvxClean.Core.Cleaning;
using IvxClean.Core.Quarantine;
using IvxClean.Core.Rules;
using IvxClean.Core.Safety;
using IvxClean.Core.Scanning;
using IvxClean.Core.Tests.Safety;
using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Cleaning;

public sealed class QuarantineTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();
    private readonly FakeClock _clock = FakeClock.AtDefault();
    private readonly QuarantineStore _store;

    public QuarantineTests() => _store = new QuarantineStore(_roots, _clock, retentionDays: 7, hardenAcls: false);

    public void Dispose() => _roots.Dispose();

    [Fact]
    public void Quarantine_then_restore_returns_identical_bytes_to_the_original_path()
    {
        string file = _roots.File("Temp/Cache/a.bin", "hello world", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        long size = new FileInfo(file).Length;
        DateTime lw = File.GetLastWriteTimeUtc(file);

        string run = _store.BeginRun("test");
        string? stored = _store.TryQuarantine(run, file, size, lw);
        _store.CommitRun(run);

        stored.ShouldNotBeNull();
        File.Exists(file).ShouldBeFalse("the original is moved, not copied");
        File.Exists(stored).ShouldBeTrue();

        RestoreResult result = _store.Restore(run, _ => RestoreConflictResolution.Skip);

        result.Restored.ShouldBe(1);
        File.Exists(file).ShouldBeTrue();
        File.ReadAllText(file).ShouldBe("hello world");
    }

    [Fact]
    public void Restore_keep_both_keeps_the_existing_file_and_restores_a_copy()
    {
        string file = _roots.File("Temp/Cache/a.bin", "original", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        string run = _store.BeginRun("test");
        _store.TryQuarantine(run, file, new FileInfo(file).Length, File.GetLastWriteTimeUtc(file));
        _store.CommitRun(run);

        // Something recreates the original path before the user restores.
        File.WriteAllText(file, "new contents");

        RestoreResult result = _store.Restore(run, _ => RestoreConflictResolution.KeepBoth);

        result.Restored.ShouldBe(1);
        File.ReadAllText(file).ShouldBe("new contents");
        string sibling = Path.Combine(Path.GetDirectoryName(file)!, "a (1).bin");
        File.Exists(sibling).ShouldBeTrue();
        File.ReadAllText(sibling).ShouldBe("original");
    }

    [Fact]
    public void Expired_runs_are_purged()
    {
        string file = _roots.File("Temp/Cache/a.bin", "x", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        string run = _store.BeginRun("test");
        _store.TryQuarantine(run, file, 1, File.GetLastWriteTimeUtc(file));
        _store.CommitRun(run);

        _store.ListRuns().ShouldHaveSingleItem();

        _clock.UtcNow = _clock.UtcNow.AddDays(8);
        int removed = _store.PurgeExpired(7);

        removed.ShouldBe(1);
        _store.ListRuns().ShouldBeEmpty();
    }

    [Fact]
    public async Task Cleaner_quarantines_a_caution_item_and_it_can_be_restored()
    {
        SafetyGuard guard = GuardFactory.Build(_roots, _clock);
        var cleaner = new Cleaner(guard, _store, _clock);

        string baseDir = _roots.Dir("Temp/Cache");
        string file = _roots.File("Temp/Cache/wu.bin", "payload", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        PlanItem item = PlanItems.ForFile(file, baseDir, RuleAction.Quarantine);

        CleanReport report = await cleaner.CleanAsync([item], new CleanOptions { Description = "Deep clean" }, null, CancellationToken.None);

        report.QuarantineRunId.ShouldNotBeNull();
        report.Items.ShouldHaveSingleItem().Outcome.ShouldBe(CleanOutcome.Quarantined);
        File.Exists(file).ShouldBeFalse();

        _store.Restore(report.QuarantineRunId!, _ => RestoreConflictResolution.Skip).Restored.ShouldBe(1);
        File.Exists(file).ShouldBeTrue();
        File.ReadAllText(file).ShouldBe("payload");
    }
}
