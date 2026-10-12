using IvxClean.Core.Safety;
using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Safety;

public sealed class ProtectedPatternsTests
{
    private static readonly ProtectedPatterns Patterns = ProtectedPatterns.LoadEmbedded();

    [Theory]
    [InlineData(@"C:\Temp\scene.blend")]
    [InlineData(@"C:\Temp\scene.blend1")]
    [InlineData(@"C:\Temp\scene.blend32")]
    [InlineData(@"C:\Temp\quit.blend")]
    [InlineData(@"C:\Temp\comp.aep")]
    [InlineData(@"C:\Temp\edit.prproj")]
    [InlineData(@"C:\Temp\art.psd")]
    [InlineData(@"C:\Temp\art.PSB")]
    [InlineData(@"C:\Temp\paint.kra")]
    [InlineData(@"C:\Temp\sculpt.ztl")]
    [InlineData(@"C:\Temp\doc.asd")]
    [InlineData(@"C:\Temp\backup.wbk")]
    [InlineData(@"C:\Temp\houdini.hipnc")]
    [InlineData(@"C:\Temp\~$report.docx")]
    public void Protected_file_names_match(string path)
        => Patterns.MatchesFileName(path).ShouldBeTrue();

    [Theory]
    [InlineData(@"C:\Temp\cache.log")]
    [InlineData(@"C:\Temp\data.bin")]
    [InlineData(@"C:\Temp\thumbcache_256.db")]
    [InlineData(@"C:\Temp\report.docx")]
    public void Ordinary_cache_files_do_not_match(string path)
        => Patterns.MatchesFileName(path).ShouldBeFalse();

    [Theory]
    [InlineData(@"C:\App\AutoRecover\file.dat")]
    [InlineData(@"C:\App\Autosave\file.dat")]
    [InlineData(@"C:\App\Auto-Save\file.dat")]
    [InlineData(@"C:\App\Recovery\file.dat")]
    [InlineData(@"C:\App\Backup\file.dat")]
    [InlineData(@"C:\App\Backups\file.dat")]
    [InlineData(@"C:\Office\UnsavedFiles\doc.tmp")]
    public void Protected_folder_names_match_anywhere_in_the_path(string path)
        => Patterns.MatchesProtectedFolder(path).ShouldBeTrue();

    [Fact]
    public void Ordinary_folders_do_not_match()
        => Patterns.MatchesProtectedFolder(@"C:\App\Cache\GPUCache\x.bin").ShouldBeFalse();
}

public sealed class SafetyGuardPatternTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();

    public void Dispose() => _roots.Dispose();

    [Fact]
    public void A_blend_file_inside_a_cleanable_temp_folder_is_protected()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        string blend = _roots.File("Temp/AppCache/scene.blend", lastWriteUtc: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(blend, new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.ProtectedPattern);
    }

    [Fact]
    public void Anything_inside_an_unsaved_files_folder_in_temp_is_protected()
    {
        string cacheBase = _roots.Dir("Temp/Office");
        string unsaved = _roots.File("Temp/Office/UnsavedFiles/draft.tmp", lastWriteUtc: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(unsaved, new SafetyTarget(cacheBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.ProtectedPattern);
    }
}
