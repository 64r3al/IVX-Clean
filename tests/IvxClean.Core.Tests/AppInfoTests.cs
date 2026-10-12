using IvxClean.Core;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests;

public class AppInfoTests
{
    [Fact]
    public void Version_is_a_non_empty_dotted_string()
    {
        AppInfo.Version.ShouldNotBeNullOrWhiteSpace();
        AppInfo.Version.ShouldContain(".");
    }

    [Fact]
    public void Branding_constants_are_stable()
    {
        AppInfo.Name.ShouldBe("IVX Clean");
        AppInfo.MadeBy.ShouldBe("IVX");
        AppInfo.RepositoryUrl.ShouldBe("https://github.com/64r3al/IVX-Clean");
    }
}
