using IvxClean.Core.Safety;
using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Safety;

public sealed class SafetyGuardPathFormTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();

    public void Dispose() => _roots.Dispose();

    [Theory]
    [InlineData(@"\\server\share\file.bin")]            // UNC / network
    [InlineData(@"\\.\PhysicalDrive0")]                  // device path
    [InlineData(@"\\?\GLOBALROOT\Device\Harddisk0")]     // device namespace
    [InlineData(@"\\?\UNC\server\share\file.bin")]       // extended UNC
    public void Network_and_device_paths_are_denied(string path)
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(path, new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.DangerousPathForm);
    }

    [Fact]
    public void Alternate_data_stream_paths_are_denied()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        string ads = Path.Combine(cacheBase, "data.bin") + ":hidden";
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(ads, new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.DangerousPathForm);
    }

    [Fact]
    public void Embedded_null_character_is_denied()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(cacheBase + "\0evil", new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.DangerousPathForm);
    }

    [Fact]
    public void Forward_slashes_and_mixed_case_resolve_correctly()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        _roots.File("Temp/AppCache/sub/data.bin", lastWriteUtc: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        string mixed = (cacheBase + "/SUB/data.bin").Replace('\\', '/');
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(mixed, new SafetyTarget(cacheBase));

        decision.IsAllowed.ShouldBeTrue(decision.Explanation);
    }

    [Fact]
    public void Trailing_dots_and_spaces_do_not_escape_the_base()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        SafetyGuard guard = GuardFactory.Build(_roots);

        // A trailing-dot sibling of the base must not be treated as inside the base.
        SafetyDecision decision = guard.Evaluate(cacheBase + "Evil", new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
    }
}
