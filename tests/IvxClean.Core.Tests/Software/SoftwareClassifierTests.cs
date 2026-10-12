using IvxClean.Core.Software;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Software;

public class SoftwareClassifierTests
{
    private static readonly SoftwareClassifier Classifier = SoftwareClassifier.LoadEmbedded();

    private static RawSoftwareEntry Entry(string name, string publisher = "")
        => new() { Id = name, DisplayName = name, Publisher = publisher, Source = SoftwareSource.Registry };

    [Theory]
    [InlineData("NVIDIA Graphics Driver 555.0", "NVIDIA Corporation")]
    [InlineData("Microsoft Visual C++ 2015-2022 Redistributable (x64)", "Microsoft Corporation")]
    [InlineData("Microsoft .NET Runtime - 10.0.0 (x64)", "Microsoft Corporation")]
    [InlineData("Easy Anti-Cheat", "EasyAntiCheat Oy")]
    [InlineData("Riot Vanguard", "Riot Games")]
    [InlineData("Microsoft Edge", "Microsoft Corporation")]
    [InlineData("Steam", "Valve")]
    [InlineData("IVX Clean", "IVX")]
    public void Drivers_runtimes_anticheat_launchers_and_self_are_never_listed(string name, string publisher)
        => Classifier.Classify(Entry(name, publisher)).ShouldBe(SoftwareClassification.NeverList);

    [Theory]
    [InlineData("WinZip Driver Updater")]
    [InlineData("Advanced SystemCare")]
    [InlineData("Driver Booster 11")]
    public void Known_bloatware_is_flagged(string name)
        => Classifier.Classify(Entry(name)).ShouldBe(SoftwareClassification.KnownBloatware);

    [Fact]
    public void A_driver_updater_is_bloatware_not_hidden_as_a_driver()
        => Classifier.Classify(Entry("WinZip Driver Updater")).ShouldBe(SoftwareClassification.KnownBloatware);

    [Theory]
    [InlineData("Some Browser Toolbar")]
    [InlineData("CCleaner")]
    public void Maybe_unwanted_is_flagged(string name)
        => Classifier.Classify(Entry(name)).ShouldBe(SoftwareClassification.MaybeUnwanted);

    [Theory]
    [InlineData("Visual Studio Code", "Microsoft Corporation")]
    [InlineData("Blender", "Blender Foundation")]
    [InlineData("7-Zip 24.0", "Igor Pavlov")]
    public void Ordinary_apps_are_kept(string name, string publisher)
        => Classifier.Classify(Entry(name, publisher)).ShouldBe(SoftwareClassification.Keep);
}
