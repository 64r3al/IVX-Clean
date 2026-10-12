using System.Text.Json.Serialization;

namespace IvxClean.Core.Rules;

/// <summary>How risky a rule is, which drives its default handling and where it appears in the UI.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RuleRisk>))]
public enum RuleRisk
{
    /// <summary>Pure self-rebuilding cache/temp/log; deleted directly.</summary>
    Safe,

    /// <summary>Rebuilds but is slower/inconvenient to lose; quarantined by default.</summary>
    Caution,

    /// <summary>Advanced/system operation; opt-in only, shown behind Expert.</summary>
    Expert,
}

/// <summary>What a rule does with the items it matches.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<RuleAction>))]
public enum RuleAction
{
    /// <summary>Delete the items directly.</summary>
    Delete,

    /// <summary>Move the items to Quarantine (restorable).</summary>
    Quarantine,

    /// <summary>Run a built-in, named operation (RecycleBin, DismComponentCleanup, …).</summary>
    Command,
}
