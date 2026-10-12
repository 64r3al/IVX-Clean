using IvxClean.Core.Abstractions;
using IvxClean.Core.Safety;
using IvxClean.TestSupport;

namespace IvxClean.Core.Tests.Safety;

/// <summary>Builds a <see cref="SafetyGuard"/> wired to a sandbox for tests.</summary>
internal static class GuardFactory
{
    public static SafetyGuard Build(
        SandboxPathRoots roots,
        IClock? clock = null,
        IEnumerable<string>? protectedUserFolders = null,
        IEnumerable<string>? userDefinedProtectedFolders = null)
    {
        SafetyPolicy policy = new(
            ProtectedPatterns.LoadEmbedded(),
            protectedUserFolders ?? [],
            userDefinedProtectedFolders ?? []);

        return new SafetyGuard(roots, clock ?? FakeClock.AtDefault(), policy);
    }
}
