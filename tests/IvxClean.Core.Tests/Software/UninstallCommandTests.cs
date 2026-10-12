using IvxClean.Core.Software;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Software;

public class UninstallCommandTests
{
    [Fact]
    public void Parses_a_quoted_path_with_arguments()
    {
        UninstallCommand? cmd = UninstallCommand.Parse("\"C:\\Program Files\\App\\uninstall.exe\" /S --mode quiet");

        cmd.ShouldNotBeNull();
        cmd.FileName.ShouldBe(@"C:\Program Files\App\uninstall.exe");
        cmd.Arguments.ShouldBe(["/S", "--mode", "quiet"]);
    }

    [Fact]
    public void Normalises_msiexec_install_guid_to_uninstall()
    {
        UninstallCommand? cmd = UninstallCommand.Parse("MsiExec.exe /I{2E8F4A3B-1111-2222-3333-444455556666}");

        cmd.ShouldNotBeNull();
        cmd.FileName.ShouldBe("MsiExec.exe");
        cmd.Arguments.ShouldHaveSingleItem().ShouldBe("/X{2E8F4A3B-1111-2222-3333-444455556666}");
    }

    [Fact]
    public void Leaves_msiexec_uninstall_guid_unchanged()
    {
        UninstallCommand? cmd = UninstallCommand.Parse("msiexec /x{2E8F4A3B-1111-2222-3333-444455556666} /quiet");

        cmd.ShouldNotBeNull();
        cmd.Arguments[0].ShouldBe("/x{2E8F4A3B-1111-2222-3333-444455556666}");
        cmd.Arguments[1].ShouldBe("/quiet");
    }

    [Fact]
    public void Handles_an_unquoted_path()
    {
        UninstallCommand? cmd = UninstallCommand.Parse(@"C:\Apps\tool\unins000.exe");

        cmd.ShouldNotBeNull();
        cmd.FileName.ShouldBe(@"C:\Apps\tool\unins000.exe");
        cmd.Arguments.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Returns_null_for_empty_input(string? input)
        => UninstallCommand.Parse(input).ShouldBeNull();
}
