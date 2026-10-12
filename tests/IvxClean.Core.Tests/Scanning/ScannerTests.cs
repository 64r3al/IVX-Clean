using System.Diagnostics;
using IvxClean.Core.Rules;
using IvxClean.Core.Safety;
using IvxClean.Core.Scanning;
using IvxClean.Core.Tests.Safety;
using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Scanning;

public sealed class ScannerTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();

    public void Dispose() => _roots.Dispose();

    private Scanner BuildScanner(RuleCatalog catalog, params string[] running)
        => new(catalog, _roots, GuardFactory.Build(_roots), new FakeProcessInspector(running));

    private static RuleCatalog CatalogWithCacheRule(int minAgeHours = 0) => RuleCatalog.Create(
    [
        new Rule
        {
            Id = "test.cache",
            Name = "Test cache",
            Category = "Temporary files",
            Level = 1,
            Risk = RuleRisk.Safe,
            Action = RuleAction.Delete,
            Why = "test",
            Sources = ["test"],
            RequiresClosed = ["game.exe"],
            Targets = [new RuleTarget { Base = "{Temp}\\PerfCache", MinAgeHours = minAgeHours }],
        },
    ]);

    [Fact]
    public async Task Scan_is_read_only_and_finds_eligible_files()
    {
        _roots.Dir("Temp/PerfCache");
        _roots.File("Temp/PerfCache/a.bin", "123", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        _roots.File("Temp/PerfCache/sub/b.bin", "4567", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        CleanPlan plan = await BuildScanner(CatalogWithCacheRule()).ScanAsync(1, false, null, CancellationToken.None);

        plan.Includable.Count(i => !i.IsDirectory).ShouldBe(2);
        File.Exists(Path.Combine(_roots.Temp, "PerfCache", "a.bin")).ShouldBeTrue("scanning must not delete anything");
    }

    [Fact]
    public async Task Running_required_process_marks_items_skipped()
    {
        _roots.File("Temp/PerfCache/a.bin", "1", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        CleanPlan plan = await BuildScanner(CatalogWithCacheRule(), "game.exe").ScanAsync(1, false, null, CancellationToken.None);

        plan.Items.ShouldAllBe(i => i.Decision == PlanDecision.Skipped);
    }

    [Fact]
    public async Task Large_tree_scans_without_freezing()
    {
        const int fileCount = 10_000;
        var old = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        string baseDir = _roots.Dir("Temp/PerfCache");
        for (int i = 0; i < fileCount; i++)
        {
            string dir = Path.Combine(baseDir, (i % 50).ToString("D2", System.Globalization.CultureInfo.InvariantCulture));
            Directory.CreateDirectory(dir);
            string f = Path.Combine(dir, $"f{i}.bin");
            File.WriteAllText(f, "x");
            File.SetLastWriteTimeUtc(f, old);
        }

        var sw = Stopwatch.StartNew();
        CleanPlan plan = await BuildScanner(CatalogWithCacheRule()).ScanAsync(1, false, null, CancellationToken.None);
        sw.Stop();

        plan.Includable.Count(i => !i.IsDirectory).ShouldBe(fileCount);
        sw.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(30), $"scanning {fileCount} files took {sw.Elapsed}");
    }

    [Fact]
    public async Task Cancellation_stops_a_scan()
    {
        for (int i = 0; i < 2000; i++)
        {
            _roots.File($"Temp/PerfCache/f{i}.bin", "x", new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        }

        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            BuildScanner(CatalogWithCacheRule()).ScanAsync(1, false, null, cts.Token));
    }
}
