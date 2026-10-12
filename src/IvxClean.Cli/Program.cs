using IvxClean.Core;

// IVX Clean CLI — scan-only in v1 (no delete capability). Made by IVX.
//
// Usage:
//   ivxclean scan --level 1..4 [--json <out.json>]
//
// The scanner and rule engine land in a later phase; this entry point establishes
// the contract and argument surface so automation/QA can depend on it.

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintUsage();
    return 0;
}

switch (args[0].ToLowerInvariant())
{
    case "scan":
        Console.Error.WriteLine($"{AppInfo.Name} {AppInfo.Version} — scan is not yet implemented in this build.");
        return 2;

    case "--version":
    case "version":
        Console.WriteLine($"{AppInfo.Name} {AppInfo.Version} — Made by {AppInfo.MadeBy}");
        return 0;

    default:
        Console.Error.WriteLine($"Unknown command: {args[0]}");
        PrintUsage();
        return 64; // EX_USAGE
}

static void PrintUsage()
{
    Console.WriteLine($"{AppInfo.Name} {AppInfo.Version} — Made by {AppInfo.MadeBy}");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  ivxclean scan --level <1-4> [--json <out.json>]   Scan only (read-only). No files are deleted.");
    Console.WriteLine("  ivxclean version                                  Print version.");
    Console.WriteLine("  ivxclean help                                     Show this help.");
}
