using IvxClean.Core.Safety;
using IvxClean.TestSupport;
using Shouldly;
using Xunit;

namespace IvxClean.Core.Tests.Safety;

/// <summary>
/// The junction attack: a cache folder that IVX Clean is allowed to clean contains a junction
/// pointing at the user's Documents. The guard must refuse the junction and anything "under" it,
/// and must never resolve a path out of the cache and into Documents.
/// Junctions do not require administrator rights, so this runs on CI.
/// </summary>
public sealed class JunctionAttackTests : IDisposable
{
    private readonly SandboxPathRoots _roots = new();

    public void Dispose() => _roots.Dispose();

    [Fact]
    public void A_junction_inside_a_cache_is_refused_and_not_followed()
    {
        string cacheBase = _roots.Dir("Temp/AppCache");
        string documents = _roots.Dir("Users/tester/Documents");
        string precious = _roots.File("Users/tester/Documents/thesis.txt", "do not delete me");

        string junction = Path.Combine(cacheBase, "evil");
        bool created = Junctions.TryCreate(junction, documents);
        created.ShouldBeTrue("the test environment must support directory junctions");

        SafetyGuard guard = GuardFactory.Build(_roots, protectedUserFolders: [documents]);
        var target = new SafetyTarget(cacheBase);

        // The junction directory itself must be refused as a reparse point.
        SafetyDecision onJunction = guard.Evaluate(junction, target);
        onJunction.IsDenied.ShouldBeTrue();
        onJunction.Reason.ShouldBe(DenyReason.ReparsePoint);

        // A path reached *through* the junction must never be allowed.
        string throughJunction = Path.Combine(junction, "thesis.txt");
        SafetyDecision onChild = guard.Evaluate(throughJunction, target);
        onChild.IsDenied.ShouldBeTrue();
        onChild.Reason.ShouldBeOneOf(DenyReason.ReparsePoint, DenyReason.OutsideBase, DenyReason.ProtectedUserFolder);

        // The guard is read-only; the precious file is untouched.
        File.Exists(precious).ShouldBeTrue();
        File.ReadAllText(precious).ShouldBe("do not delete me");
    }

    [Fact]
    public void A_junction_as_the_rule_base_resolves_to_its_target_and_is_caught()
    {
        // If a rule's base folder is itself swapped for a junction into Documents, every item
        // resolves into Documents and must be denied as a protected user folder, never cleaned.
        string documents = _roots.Dir("Users/tester/Documents");
        string precious = _roots.File("Users/tester/Documents/budget.xlsx", "money");

        string fakeBase = Path.Combine(_roots.Dir("Temp"), "LooksLikeCache");
        bool created = Junctions.TryCreate(fakeBase, documents);
        created.ShouldBeTrue("the test environment must support directory junctions");

        SafetyGuard guard = GuardFactory.Build(_roots, protectedUserFolders: [documents]);

        SafetyDecision decision = guard.Evaluate(Path.Combine(fakeBase, "budget.xlsx"), new SafetyTarget(fakeBase));

        decision.IsDenied.ShouldBeTrue();
        File.Exists(precious).ShouldBeTrue();
    }
}
