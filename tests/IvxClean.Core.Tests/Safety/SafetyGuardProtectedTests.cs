using IvxClean.Core.Safety;
using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Safety;

public sealed class SafetyGuardProtectedTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();

    public void Dispose() => _roots.Dispose();

    [Fact]
    public void Windows_system_folders_are_protected()
    {
        string file = _roots.File("Windows/System32/drivers/etc/hosts");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(_roots.Windows));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.ProtectedSystemLocation);
    }

    [Fact]
    public void The_drive_root_is_denied_even_with_base_deletion_opted_in()
    {
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(_roots.SystemDrive, new SafetyTarget(_roots.SystemDrive, AllowBaseDeletion: true));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.ProtectedSystemLocation);
    }

    [Theory]
    [InlineData("ProgramData")]
    [InlineData("UserProfile")]
    [InlineData("LocalAppData")]
    [InlineData("RoamingAppData")]
    public void Container_roots_cannot_be_deleted_as_a_whole(string which)
    {
        string root = which switch
        {
            "ProgramData" => _roots.ProgramData,
            "UserProfile" => _roots.UserProfile,
            "LocalAppData" => _roots.LocalAppData,
            "RoamingAppData" => _roots.RoamingAppData,
            _ => throw new ArgumentOutOfRangeException(nameof(which)),
        };
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(root, new SafetyTarget(root, AllowBaseDeletion: true));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.ProtectedSystemLocation);
    }

    [Fact]
    public void A_cache_inside_a_container_root_is_still_allowed()
    {
        string cacheBase = _roots.Dir("LocalAppData/SomeApp/Cache");
        string file = _roots.File("LocalAppData/SomeApp/Cache/entry.bin", lastWriteUtc: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(cacheBase));

        decision.IsAllowed.ShouldBeTrue(decision.Explanation);
    }

    [Fact]
    public void Ivx_cleans_own_data_folder_is_protected()
    {
        string file = _roots.File("LocalAppData/IVXClean/logs/run.log");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(_roots.LocalAppData));

        decision.IsDenied.ShouldBeTrue();
    }

    [Theory]
    [InlineData("drive")]
    [InlineData("windows")]
    [InlineData("localappdata")]
    [InlineData("profile")]
    public void A_rule_base_that_is_a_broad_root_is_refused(string which)
    {
        string baseFolder = which switch
        {
            "drive" => _roots.SystemDrive,
            "windows" => _roots.Windows,
            "localappdata" => _roots.LocalAppData,
            "profile" => _roots.UserProfile,
            _ => throw new ArgumentOutOfRangeException(nameof(which)),
        };
        // A file sits directly inside the broad root — it must NOT be cleanable.
        string file = Path.Combine(baseFolder, "victim.bin");
        File.WriteAllText(file, "x");
        SafetyGuard guard = GuardFactory.Build(_roots);

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(baseFolder));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.ProtectedSystemLocation);
    }

    [Fact]
    public void Protected_user_folders_are_denied()
    {
        // A protected personal folder redirected to sit inside an otherwise-cleanable location.
        string cleanBase = _roots.Dir("LocalAppData/App/Data");
        string documents = _roots.Dir("LocalAppData/App/Data/Documents");
        string file = _roots.File("LocalAppData/App/Data/Documents/thesis.txt");
        SafetyGuard guard = GuardFactory.Build(_roots, protectedUserFolders: [documents]);

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(cleanBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.ProtectedUserFolder);
    }

    [Fact]
    public void User_defined_protected_folders_are_denied()
    {
        string cleanBase = _roots.Dir("LocalAppData/App/Data");
        string secrets = _roots.Dir("LocalAppData/App/Data/MyGameSaves");
        string file = _roots.File("LocalAppData/App/Data/MyGameSaves/slot1.sav");
        SafetyGuard guard = GuardFactory.Build(_roots, userDefinedProtectedFolders: [secrets]);

        SafetyDecision decision = guard.Evaluate(file, new SafetyTarget(cleanBase));

        decision.IsDenied.ShouldBeTrue();
        decision.Reason.ShouldBe(DenyReason.UserProtectedFolder);
    }
}
