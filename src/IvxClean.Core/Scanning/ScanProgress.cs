namespace IvxClean.Core.Scanning;

/// <summary>Progress reported as a scan runs, so the UI can show live category/path/size.</summary>
public sealed record ScanProgress
{
    /// <summary>The category currently being scanned.</summary>
    public required string Category { get; init; }

    /// <summary>The path currently being examined (for the scrolling mono line).</summary>
    public required string CurrentPath { get; init; }

    /// <summary>Total includable bytes found so far.</summary>
    public required long BytesFound { get; init; }

    /// <summary>Total items examined so far.</summary>
    public required long ItemsExamined { get; init; }

    /// <summary>Completed rule count, for the progress ring.</summary>
    public required int RulesCompleted { get; init; }

    /// <summary>Total rules in this scan.</summary>
    public required int RulesTotal { get; init; }
}
