using IvxClean.Core.Rules;
using Shouldly;
using Xunit;

namespace IvxClean.Rules.Tests;

public class CatalogTests
{
    private static readonly RuleCatalog Catalog = RuleCatalog.LoadEmbedded();

    [Fact]
    public void Catalog_loads_rules_from_the_embedded_json()
        => Catalog.Rules.Count.ShouldBeGreaterThan(10);

    [Fact]
    public void Every_rule_passes_validation()
    {
        IReadOnlyList<string> errors = RuleValidator.Validate(Catalog);
        errors.ShouldBeEmpty();
    }

    [Fact]
    public void Rule_ids_are_unique()
    {
        IEnumerable<IGrouping<string, Rule>> dupes = Catalog.Rules
            .GroupBy(r => r.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1);

        dupes.ShouldBeEmpty();
    }

    [Fact]
    public void Every_rule_has_a_why_and_at_least_one_source()
    {
        foreach (Rule rule in Catalog.Rules)
        {
            rule.Why.ShouldNotBeNullOrWhiteSpace();
            rule.Sources.ShouldNotBeEmpty();
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Each_level_has_at_least_one_rule(int level)
        => Catalog.Rules.ShouldContain(r => r.Level == level);

    [Fact]
    public void Level_filtering_hides_expert_rules_unless_opted_in()
    {
        Catalog.ForLevel(4, includeExpert: false).ShouldNotContain(r => r.Risk == RuleRisk.Expert);
        Catalog.ForLevel(4, includeExpert: true).ShouldContain(r => r.Risk == RuleRisk.Expert);
    }

    [Fact]
    public void Quick_level_only_exposes_safe_rules()
        => Catalog.ForLevel(1, includeExpert: false).ShouldAllBe(r => r.Risk == RuleRisk.Safe);
}
