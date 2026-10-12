using IvxClean.Core.Abstractions;
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

public sealed class CleanerTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();
    private readonly FakeClock _clock = FakeClock.AtDefault();

    public void Dispose() => _roots.Dispose();

    private Cleaner BuildCleaner()
    {
        SafetyGuard guard = GuardFactory.Build(_roots, _clock);
        var quarantine = new QuarantineStore(_roots, _clock, retentionDays: 7, hardenAcls: false);
        return new Cleaner(guard, quarantine, _clock);
    }

    [Fact]
    public async Task Deletes_an_includable_file()
    {
        string baseDir = _roots.Dir("Temp/Cache");
        string file = _roots.File("Temp/Cache/a.bin", "data", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        PlanItem item = PlanItems.ForFile(file, baseDir);

        CleanReport report = await BuildCleaner().CleanAsync([item], new CleanOptions(), null, CancellationToken.None);

        File.Exists(file).ShouldBeFalse();
        report.Items.ShouldHaveSingleItem().Outcome.ShouldBe(CleanOutcome.Deleted);
        report.BytesFreed.ShouldBe(4);
    }

    [Fact]
    public async Task Never_deletes_the_base_folder()
    {
        string baseDir = _roots.Dir("Temp/Cache");
        string file = _roots.File("Temp/Cache/a.bin", "x", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        await BuildCleaner().CleanAsync([PlanItems.ForFile(file, baseDir)], new CleanOptions(), null, CancellationToken.None);

        Directory.Exists(baseDir).ShouldBeTrue("the rule base folder must survive");
    }

    [Fact]
    public async Task Removes_empty_subdirectories_bottom_up_but_keeps_the_base()
    {
        string baseDir = _roots.Dir("Temp/Cache");
        string file = _roots.File("Temp/Cache/sub/deep/a.bin", "x", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        await BuildCleaner().CleanAsync([PlanItems.ForFile(file, baseDir)], new CleanOptions(), null, CancellationToken.None);

        Directory.Exists(Path.Combine(baseDir, "sub", "deep")).ShouldBeFalse();
        Directory.Exists(Path.Combine(baseDir, "sub")).ShouldBeFalse();
        Directory.Exists(baseDir).ShouldBeTrue();
    }

    [Fact]
    public async Task A_file_in_use_is_skipped_and_kept()
    {
        string baseDir = _roots.Dir("Temp/Cache");
        string file = _roots.File("Temp/Cache/locked.bin", "x", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        PlanItem item = PlanItems.ForFile(file, baseDir);

        using (new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            CleanReport report = await BuildCleaner().CleanAsync([item], new CleanOptions(), null, CancellationToken.None);
            report.Items.ShouldHaveSingleItem().Outcome.ShouldBe(CleanOutcome.Skipped);
        }

        File.Exists(file).ShouldBeTrue();
    }

    [Fact]
    public async Task A_read_only_file_is_deleted()
    {
        string baseDir = _roots.Dir("Temp/Cache");
        string file = _roots.File("Temp/Cache/ro.bin", "x", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        File.SetAttributes(file, FileAttributes.ReadOnly);
        PlanItem item = PlanItems.ForFile(file, baseDir);

        await BuildCleaner().CleanAsync([item], new CleanOptions(), null, CancellationToken.None);

        File.Exists(file).ShouldBeFalse();
    }

    [Fact]
    public async Task A_file_changed_since_the_scan_is_skipped()
    {
        string baseDir = _roots.Dir("Temp/Cache");
        string file = _roots.File("Temp/Cache/race.bin", "x", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        PlanItem item = PlanItems.ForFile(file, baseDir);

        // Simulate a writer touching the file after the scan captured it.
        File.WriteAllText(file, "changed");
        File.SetLastWriteTimeUtc(file, new DateTime(2026, 5, 5, 0, 0, 0, DateTimeKind.Utc));

        CleanReport report = await BuildCleaner().CleanAsync([item], new CleanOptions(), null, CancellationToken.None);

        report.Items.ShouldHaveSingleItem().Outcome.ShouldBe(CleanOutcome.Skipped);
        File.Exists(file).ShouldBeTrue();
    }

    [Fact]
    public async Task Cancellation_produces_a_partial_report_and_leaves_remaining_files()
    {
        string baseDir = _roots.Dir("Temp/Cache");
        var items = new List<PlanItem>();
        for (int i = 0; i < 5; i++)
        {
            items.Add(PlanItems.ForFile(_roots.File($"Temp/Cache/f{i}.bin", "x", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc)), baseDir));
        }

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        CleanReport report = await BuildCleaner().CleanAsync(items, new CleanOptions(), null, cts.Token);

        report.Cancelled.ShouldBeTrue();
        report.Items.Count(i => i.Outcome == CleanOutcome.Deleted).ShouldBeLessThan(5);
    }
}
