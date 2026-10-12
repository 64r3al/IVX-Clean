using System.Text.Json;
using System.Text.Json.Serialization;
using IvxClean.Core;
using IvxClean.Core.Abstractions;
using IvxClean.Core.Formatting;
using IvxClean.Core.Paths;
using IvxClean.Core.Rules;
using IvxClean.Core.Safety;
using IvxClean.Core.Scanning;

// IVX Clean CLI — scan-only in v1 (no delete capability). Made by IVX.
//
// Usage:
//   ivxclean scan --level 1..4 [--json <out.json>] [--expert]

if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
{
    PrintUsage();
    return 0;
}

switch (args[0].ToLowerInvariant())
{
    case "scan":
        return await RunScanAsync(args).ConfigureAwait(false);

    case "--version":
    case "version":
        Console.WriteLine($"{AppInfo.Name} {AppInfo.Version} — Made by {AppInfo.MadeBy}");
        return 0;

    default:
        Console.Error.WriteLine($"Unknown command: {args[0]}");
        PrintUsage();
        return 64; // EX_USAGE
}

static async Task<int> RunScanAsync(string[] args)
{
    int level = 2;
    bool expert = false;
    string? jsonOut = null;

    for (int i = 1; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--level" when i + 1 < args.Length && int.TryParse(args[i + 1], out int parsed):
                level = Math.Clamp(parsed, 1, 4);
                i++;
                break;
            case "--json" when i + 1 < args.Length:
                jsonOut = args[++i];
                break;
            case "--expert":
                expert = true;
                break;
            default:
                Console.Error.WriteLine($"Unknown option: {args[i]}");
                return 64;
        }
    }

    var roots = new ProductionPathRoots();
    var policy = SafetyPolicy.CreateProduction();
    var guard = new SafetyGuard(roots, new SystemClock(), policy);
    RuleCatalog catalog = RuleCatalog.LoadEmbedded();
    var scanner = new Scanner(catalog, roots, guard, new ProcessInspector());

    Console.WriteLine($"{AppInfo.Name} {AppInfo.Version} — scanning at level {level}{(expert ? " (+Expert)" : string.Empty)}. Read-only; nothing is deleted.");
    Console.WriteLine();

    CleanPlan plan = await scanner.ScanAsync(level, expert, progress: null, CancellationToken.None).ConfigureAwait(false);

    var byCategory = plan.Includable
        .GroupBy(i => i.Category)
        .Select(g => (Category: g.Key, Bytes: g.Sum(i => i.Size), Count: g.Count()))
        .OrderByDescending(g => g.Bytes)
        .ToList();

    Console.WriteLine($"{"Category",-28}{"Items",10}{"Size",16}");
    Console.WriteLine(new string('-', 54));
    foreach ((string category, long bytes, int count) in byCategory)
    {
        Console.WriteLine($"{category,-28}{count,10}{ByteSize.Humanize(bytes),16}");
    }

    Console.WriteLine(new string('-', 54));
    Console.WriteLine($"{"Includable total",-28}{plan.Includable.Count(),10}{ByteSize.Humanize(plan.IncludableBytes),16}");
    Console.WriteLine();
    Console.WriteLine($"Protected items: {plan.ProtectedCount}   Skipped items: {plan.SkippedCount}");

    if (jsonOut is not null)
    {
        var report = new ScanReportDto
        {
            Product = AppInfo.Name,
            Version = AppInfo.Version,
            Level = plan.Level,
            IncludedExpert = plan.IncludedExpert,
            IncludableBytes = plan.IncludableBytes,
            ProtectedCount = plan.ProtectedCount,
            SkippedCount = plan.SkippedCount,
            Categories = [.. byCategory.Select(c => new CategoryDto { Category = c.Category, Items = c.Count, Bytes = c.Bytes })],
        };
        await File.WriteAllTextAsync(jsonOut, JsonSerializer.Serialize(report, CliJsonContext.Default.ScanReportDto)).ConfigureAwait(false);
        Console.WriteLine($"\nJSON report written to {jsonOut}");
    }

    return 0;
}

static void PrintUsage()
{
    Console.WriteLine($"{AppInfo.Name} {AppInfo.Version} — Made by {AppInfo.MadeBy}");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  ivxclean scan --level <1-4> [--expert] [--json <out.json>]   Scan only (read-only). No files are deleted.");
    Console.WriteLine("  ivxclean version                                            Print version.");
    Console.WriteLine("  ivxclean help                                               Show this help.");
}

internal sealed class ScanReportDto
{
    public required string Product { get; init; }
    public required string Version { get; init; }
    public required int Level { get; init; }
    public required bool IncludedExpert { get; init; }
    public required long IncludableBytes { get; init; }
    public required int ProtectedCount { get; init; }
    public required int SkippedCount { get; init; }
    public required IReadOnlyList<CategoryDto> Categories { get; init; }
}

internal sealed class CategoryDto
{
    public required string Category { get; init; }
    public required int Items { get; init; }
    public required long Bytes { get; init; }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ScanReportDto))]
internal sealed partial class CliJsonContext : JsonSerializerContext;
