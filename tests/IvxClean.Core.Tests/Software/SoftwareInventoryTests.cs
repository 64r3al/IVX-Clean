using IvxClean.Core.Software;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Software;

public class SoftwareInventoryFilterTests
{
    private static RegistryUninstallRecord Record(
        string? name = "An App", int? systemComponent = null, string? parent = null,
        string? releaseType = null, string? uninstall = "u.exe", int? windowsInstaller = null)
        => new()
        {
            KeyId = "k",
            DisplayName = name,
            SystemComponent = systemComponent,
            ParentKeyName = parent,
            ReleaseType = releaseType,
            UninstallString = uninstall,
            WindowsInstaller = windowsInstaller,
        };

    [Fact]
    public void Includes_a_normal_program() => SoftwareInventoryFilter.ShouldInclude(Record()).ShouldBeTrue();

    [Fact]
    public void Excludes_entries_without_a_display_name()
        => SoftwareInventoryFilter.ShouldInclude(Record(name: null)).ShouldBeFalse();

    [Fact]
    public void Excludes_system_components()
        => SoftwareInventoryFilter.ShouldInclude(Record(systemComponent: 1)).ShouldBeFalse();

    [Fact]
    public void Excludes_child_components_with_a_parent_key()
        => SoftwareInventoryFilter.ShouldInclude(Record(parent: "SomeParent")).ShouldBeFalse();

    [Theory]
    [InlineData("Update")]
    [InlineData("Hotfix")]
    [InlineData("Security Update")]
    public void Excludes_updates_and_hotfixes(string releaseType)
        => SoftwareInventoryFilter.ShouldInclude(Record(releaseType: releaseType)).ShouldBeFalse();

    [Fact]
    public void Excludes_entries_with_no_way_to_uninstall()
        => SoftwareInventoryFilter.ShouldInclude(Record(uninstall: null)).ShouldBeFalse();

    [Fact]
    public void Includes_msi_products_even_without_an_uninstall_string()
        => SoftwareInventoryFilter.ShouldInclude(Record(uninstall: null, windowsInstaller: 1)).ShouldBeTrue();
}

public class SoftwareInventoryTests
{
    private sealed class FakeSource(params RawSoftwareEntry[] entries) : IInstalledSoftwareSource
    {
        public IReadOnlyList<RawSoftwareEntry> GetRaw() => entries;
    }

    private static RawSoftwareEntry Raw(string name, string publisher = "")
        => new() { Id = name, DisplayName = name, Publisher = publisher, Source = SoftwareSource.Registry };

    [Fact]
    public void Never_listed_items_are_hidden_from_every_group()
    {
        var source = new FakeSource(
            Raw("NVIDIA Graphics Driver", "NVIDIA Corporation"),
            Raw("IVX Clean", "IVX"),
            Raw("Driver Booster 11"),
            Raw("CCleaner"),
            Raw("Blender", "Blender Foundation"));

        SoftwareInventoryResult result = new SoftwareInventory([source], SoftwareClassifier.LoadEmbedded()).Build();

        result.KnownBloatware.ShouldContain(e => e.DisplayName == "Driver Booster 11");
        result.MaybeUnwanted.ShouldContain(e => e.DisplayName == "CCleaner");
        result.Keep.ShouldContain(e => e.DisplayName == "Blender");

        IEnumerable<string> all = result.KnownBloatware.Concat(result.MaybeUnwanted).Concat(result.Keep).Select(e => e.DisplayName);
        all.ShouldNotContain("NVIDIA Graphics Driver");
        all.ShouldNotContain("IVX Clean");
    }

    [Fact]
    public void Duplicate_entries_are_de_duplicated()
    {
        var source = new FakeSource(Raw("7-Zip"), Raw("7-Zip"));

        SoftwareInventoryResult result = new SoftwareInventory([source], SoftwareClassifier.LoadEmbedded()).Build();

        result.Keep.Count(e => e.DisplayName == "7-Zip").ShouldBe(1);
    }
}
