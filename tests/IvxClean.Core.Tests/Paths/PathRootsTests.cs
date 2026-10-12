using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Paths;

public sealed class PathRootsTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();

    public void Dispose() => _roots.Dispose();

    [Fact]
    public void Single_value_token_expands_with_remainder()
    {
        IReadOnlyList<string> result = _roots.Expand(@"{Temp}\Google\Chrome");

        result.Count.ShouldBe(1);
        result[0].ShouldBe(Path.Combine(_roots.Temp, "Google", "Chrome"));
    }

    [Fact]
    public void Each_fixed_drive_token_expands_per_drive()
    {
        IReadOnlyList<string> result = _roots.Expand(@"{EachFixedDrive}\Cache");

        result.ShouldContain(Path.Combine(_roots.SystemDrive, "Cache"));
    }

    [Fact]
    public void Each_user_profile_token_expands_per_profile()
    {
        IReadOnlyList<string> result = _roots.Expand(@"{EachUserProfile}\AppData\Local");

        result.ShouldContain(Path.Combine(_roots.UserProfile, "AppData", "Local"));
    }

    [Fact]
    public void A_bare_token_expands_to_the_root_itself()
        => _roots.Expand("{Temp}").ShouldBe([_roots.Temp]);

    [Fact]
    public void A_literal_path_is_returned_unchanged()
        => _roots.Expand(@"C:\literal\path").ShouldBe([@"C:\literal\path"]);

    [Fact]
    public void An_unknown_token_throws()
        => Should.Throw<InvalidOperationException>(() => _roots.Expand("{NoSuchToken}\\x"));
}
