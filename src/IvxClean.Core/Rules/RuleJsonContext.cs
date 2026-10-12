using System.Text.Json;
using System.Text.Json.Serialization;

namespace IvxClean.Core.Rules;

/// <summary>A catalog file: a named set of rules.</summary>
public sealed class RuleCatalogFile
{
    /// <summary>The rules declared in this file.</summary>
    [JsonPropertyName("rules")] public IReadOnlyList<Rule> Rules { get; init; } = [];
}

/// <summary>Source-generated JSON handling for the rule catalog (trim/AOT friendly, fast).</summary>
[JsonSourceGenerationOptions(
    PropertyNameCaseInsensitive = true,
    ReadCommentHandling = JsonCommentHandling.Skip,
    AllowTrailingCommas = true)]
[JsonSerializable(typeof(RuleCatalogFile))]
[JsonSerializable(typeof(Rule))]
public sealed partial class RuleJsonContext : JsonSerializerContext;
