using System;
using System.Linq;
using IvxClean.App.ViewModels;
using IvxClean.Core.Rules;
using IvxClean.Core.Scanning;
using Shouldly;
using Xunit;

namespace IvxClean.App.Tests;

public class ReviewViewModelTests
{
    private static PlanItem Item(string category, RuleRisk risk, RuleAction action, long size, PlanDecision decision = PlanDecision.Include)
        => new()
        {
            RuleId = "r",
            Category = category,
            Risk = risk,
            Action = action,
            Path = $@"C:\Temp\{Guid.NewGuid():N}.bin",
            BaseFolder = @"C:\Temp",
            MinAgeHours = 0,
            AllowBaseDeletion = false,
            IsDirectory = false,
            Size = size,
            LastWriteUtc = DateTime.UtcNow,
            Decision = decision,
            Reason = "x",
        };

    private static CleanPlan Plan() => new(2, false,
    [
        Item("Browser caches", RuleRisk.Safe, RuleAction.Delete, 100),
        Item("Browser caches", RuleRisk.Safe, RuleAction.Delete, 200),
        Item("Windows Update", RuleRisk.Caution, RuleAction.Quarantine, 300),
        Item("Windows", RuleRisk.Safe, RuleAction.Delete, 999, PlanDecision.Protected),
        Item("Temporary files", RuleRisk.Safe, RuleAction.Delete, 50, PlanDecision.Skipped),
    ]);

    [Fact]
    public void All_includable_items_start_selected_with_correct_totals()
    {
        var vm = new ReviewViewModel(Plan());

        vm.FoundCount.ShouldBe(3);
        vm.SelectedCount.ShouldBe(3);
        vm.SelectedBytes.ShouldBe(600);
        vm.DeleteBytes.ShouldBe(300);
        vm.QuarantineBytes.ShouldBe(300);
        vm.SkippedOrProtectedCount.ShouldBe(2);
        vm.HasSelection.ShouldBeTrue();
    }

    [Fact]
    public void Deselecting_an_item_updates_the_totals()
    {
        var vm = new ReviewViewModel(Plan());
        PlanItemViewModel first = vm.AllItems.First(i => i.IsIncludable && i.Item.Size == 100);

        first.IsSelected = false;

        vm.SelectedCount.ShouldBe(2);
        vm.SelectedBytes.ShouldBe(500);
    }

    [Fact]
    public void Protected_and_skipped_items_are_not_selectable()
    {
        var vm = new ReviewViewModel(Plan());
        vm.AllItems.Where(i => i.Decision != PlanDecision.Include).ShouldAllBe(i => !i.IsIncludable);
    }

    [Fact]
    public void Filtering_to_caution_hides_safe_items()
    {
        var vm = new ReviewViewModel(Plan()) { Filter = ReviewFilter.Caution };

        vm.AllItems.Where(i => i.Item.Risk == RuleRisk.Caution).ShouldAllBe(i => i.IsVisible);
        vm.AllItems.Where(i => i.Item.Risk == RuleRisk.Safe).ShouldAllBe(i => !i.IsVisible);
    }

    [Fact]
    public void Select_all_on_a_group_toggles_only_that_group()
    {
        var vm = new ReviewViewModel(Plan());
        RuleGroupViewModel browsers = vm.Groups.First(g => g.Category == "Browser caches");

        browsers.SetAll(false);

        browsers.Includable.ShouldAllBe(i => !i.IsSelected);
        browsers.AllSelected.ShouldBe(false);
        // The caution group is untouched.
        vm.SelectedCount.ShouldBe(1);
        vm.SelectedBytes.ShouldBe(300);
    }

    [Fact]
    public void Group_header_is_indeterminate_when_partially_selected()
    {
        var vm = new ReviewViewModel(Plan());
        RuleGroupViewModel browsers = vm.Groups.First(g => g.Category == "Browser caches");

        browsers.Includable.First().IsSelected = false;

        browsers.AllSelected.ShouldBeNull();
    }
}
