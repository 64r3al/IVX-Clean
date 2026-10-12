using IvxClean.Core.Safety;
using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Safety;

public sealed class SafetyGuardCoreTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();

    public void Dispose() => _roots.Dispose();

    [Fact]
    public void Ordinary_old_file_inside_base_is_allowed()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        string file = _roots.File("Temp/AppCache/data.bin", lastWriteUtc: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(cacheBase));

        decision.IsAllowed.ShouldBeTrue(decision.Explanation);
    }

    [Fact]
    public void The_base_folder_itself_is_never_deleted()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(cacheBase, new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.BaseFolder);
    }

    [Fact]
    public void The_base_folder_may_be_deleted_when_the_rule_opts_in()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(cacheBase, new SafetyTarget(cacheBase, AllowBaseDeletion: true));

        decision.IsAllowed.ShouldBeTrue(decision.Explanation);
    }

    [Fact]
    public void Item_outside_the_base_is_denied()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        string elsewhere = _roots.File("LocalAppData/other.bin");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(elsewhere, new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.OutsideBase);
    }

    [Fact]
    public void Dot_dot_traversal_escaping_the_base_is_denied()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        _roots.Dir("Windows/System32");
        string escaping = Path.Combine(cacheBase, "..", "..", "Windows", "System32");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(escaping, new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
    }

    [Fact]
    public void A_vanished_item_is_denied_not_crashed_on()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        string ghost = Path.Combine(cacheBase, "not-there.bin");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(ghost, new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.Unresolvable);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    public void File_newer_than_min_age_is_denied(int ageHours)
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        string cacheBase = _roots.Dir("Temp/AppCache");
        string file = _roots.File("Temp/AppCache/fresh.log", lastWriteUtc: now.AddHours(-ageHours + 0.5));
        SafetyGuard guard = GuardFactory.Build(_roots, new FakeClock(now));

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(cacheBase, MinAgeHours: ageHours));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.TooRecent);
    }

    [Fact]
    public void File_older_than_min_age_survives()
    {
        var now = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc);
        string cacheBase = _roots.Dir("Temp/AppCache");
        string file = _roots.File("Temp/AppCache/old.log", lastWriteUtc: now.AddHours(-48));
        SafetyGuard guard = GuardFactory.Build(_roots, new FakeClock(now));

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(cacheBase, MinAgeHours: 24));

        decision.IsAllowed.ShouldBeTrue(decision.Explanation);
    }

    [Theory]
    [InlineData("café-résumé.tmp")]
    [InlineData("ملف-مؤقت.log")]
    [InlineData("cache-😀-data.bin")]
    public void Unicode_file_names_are_handled(string name)
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        string file = _roots.File($"Temp/AppCache/{name}", lastWriteUtc: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(cacheBase));

        decision.IsAllowed.ShouldBeTrue(decision.Explanation);
    }

    [Fact]
    public void Long_paths_over_260_characters_are_handled()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        // Build a path comfortably past MAX_PATH.
        string deep = cacheBase;
        for (int i = 0; i < 12 && deep.Length < 320; i++)
        {
            deep = Path.Combine(deep, new string((char)('a' + i), 24));
        }

        Directory.CreateDirectory(ToExtended(deep));
        string file = Path.Combine(deep, "payload.bin");
        File.WriteAllText(ToExtended(file), "x");
        File.SetLastWriteTimeUtc(ToExtended(file), new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        file.Length.ShouldBeGreaterThan(260);

        SafetyGuard guard = GuardFactory.Build(_roots);
        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(cacheBase));

        decision.IsAllowed.ShouldBeTrue(decision.Explanation);
    }

    private static string ToExtended(string path) => @"\\?\" + path;
}
