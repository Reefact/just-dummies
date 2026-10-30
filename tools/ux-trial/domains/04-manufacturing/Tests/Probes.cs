using System.Text.RegularExpressions;
using JustDummies;
using JustDummies.Xunit;
using Manufacturing.Domain;
using Xunit;

namespace Manufacturing.Tests;

/// <summary>Deliberate edge cases. Each test pins the behaviour observed, so a change shows up here.</summary>
public sealed class Probes {

    // ---- Contradictions: fast, and naming both sides -------------------------------------------

    [Fact]
    public void Crossing_TimeSpan_bounds_name_both_calls() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.TimeSpan().Between(TimeSpan.FromHours(1), TimeSpan.FromHours(2)).LessThan(TimeSpan.FromMinutes(30)));

        Assert.Equal("Cannot apply LessThan(00:30:00) because Between(01:00:00, 02:00:00) already requires values greater than or equal to 01:00:00.", conflict.Message);
    }

    [Fact]
    public void A_granularity_with_no_point_in_range_is_refused_at_declaration() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.TimeSpan().Between(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(50)).WithGranularity(TimeSpan.FromMinutes(1)));

        Assert.Contains("WithGranularity(00:01:00)", conflict.Message);
        Assert.Contains("between 00:00:10 and 00:00:50", conflict.Message);
    }

    [Fact]
    public void A_scale_with_no_value_between_equal_bounds_is_refused() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Decimal().Between(1.5m, 1.5m).WithScale(0));

        Assert.Equal("Cannot apply WithScale(0) because no Decimal value WithScale(0) allows remains between 1.5 and 1.5.", conflict.Message);
    }

    [Fact]
    public void Excluding_every_machine_state_is_refused_by_name() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Enum<MachineState>().Except(Enum.GetValues<MachineState>()).Generate());

        Assert.Contains("forbids every declared MachineState member", conflict.Message);
    }

    [Fact]
    public void A_batch_prefix_longer_than_the_length_is_refused() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.String().StartingWith("LYN-").WithLength(3));

        Assert.Equal("Cannot apply WithLength(3) because the prefix \"LYN-\" already requires 4 characters.", conflict.Message);
    }

    // ---- Bounds -----------------------------------------------------------------------------------

    [Fact]
    public void Equal_bounds_yield_that_value() {
        Assert.Equal(TimeSpan.FromMinutes(7), Any.TimeSpan().Between(TimeSpan.FromMinutes(7), TimeSpan.FromMinutes(7)).Generate());
        Assert.Equal(5, Any.Int32().Between(5, 5).Generate());
    }

    [Fact]
    public void WithScale_3_always_carries_exactly_three_decimal_places() {
        for (int draw = 0; draw < 2_000; draw++) {
            Assert.Equal(3, Any.Decimal().Between(1m, 5_000m).WithScale(3).Generate().Scale);
        }
    }

    [Fact]
    public void WithScale_29_is_an_argument_error() {
        Assert.Throws<ArgumentOutOfRangeException>("scale", () => Any.Decimal().WithScale(29));
    }

    [Fact]
    public void DateTime_one_tick_below_MaxValue_draws_MaxValue() {
        Assert.Equal(DateTime.MaxValue.Ticks, Any.DateTime().After(DateTime.MaxValue.AddTicks(-1)).Generate().Ticks);
    }

    [Fact]
    public void DateTime_after_MaxValue_is_a_conflict() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateTime().After(DateTime.MaxValue));
    }

    [Fact]
    public void A_local_DateTime_bound_is_refused() {
        DateTime localStart = new DateTime(2026, 3, 1, 6, 0, 0, DateTimeKind.Local);

        // A Local bound used to be read by its ticks and relabelled Utc (see NOTES.md); since ADR-0114 it is refused.
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.DateTime().Between(localStart, localStart.AddHours(8)));

        Assert.Contains("ToUniversalTime()", refusal.Message);
    }

    [Fact]
    public void A_night_shift_crossing_midnight_cannot_be_expressed_as_a_TimeOnly_range() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(6, 0)));

        Assert.Equal("The start (22:00:00.0000000) must be at or before the end (06:00:00.0000000). (Parameter 'start')", refusal.Message);
    }

    [Fact]
    public void A_night_shift_is_drawn_as_an_offset_from_its_start_instead() {
        TimeOnly nightStart = new TimeOnly(22, 0);

        TimeOnly anyNightTime = nightStart.Add(Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromHours(8)).WithGranularity(TimeSpan.FromMinutes(1)).Generate());

        Assert.True(anyNightTime >= nightStart || anyNightTime <= new TimeOnly(6, 0));
    }

    [Fact]
    public void An_unconstrained_TimeSpan_spans_tens_of_thousands_of_years_both_signs() {
        List<TimeSpan> anyDurations = Any.ListOf(Any.TimeSpan()).WithCount(50).Generate();

        Assert.Contains(anyDurations, d => d < TimeSpan.Zero);
        Assert.Contains(anyDurations, d => d > TimeSpan.FromDays(365));
    }

    // ---- Patterns -------------------------------------------------------------------------------

    private const string BatchFormat = @"^(?<plant>[A-Z]{3})-(?<year>\d{2})(?<week>0[1-9]|[1-4]\d|5[0-3])-(?<sequence>\d{4})$";

    [Fact]
    public void The_batch_pattern_with_named_groups_and_alternation_always_matches() {
        AnyPattern anyBatchText = Any.StringMatching(BatchFormat);

        for (int draw = 0; draw < 2_000; draw++) {
            Assert.Matches(BatchFormat, anyBatchText.Generate());
        }
    }

    [Theory]
    [InlineData(@"[A-Z-[IO]]{3}", "character-class subtraction")]
    [InlineData(@"(?!0000)\d{4}", "negative lookahead")]
    [InlineData(@"\p{Lu}{3}", "Unicode category")]
    [InlineData(@"\bM-\d{3}\b", "word-boundary")]
    [InlineData(@"(\d)\1", "backreference")]
    public void Constructs_outside_the_regular_subset_are_refused_by_name(string pattern, string construct) {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(pattern));

        Assert.Contains(construct, refusal.Message);
    }

    [Fact]
    public void IgnorePatternWhitespace_is_refused_as_an_argument_error() {
        Assert.Throws<ArgumentException>("pattern", () => Any.StringMatching(new Regex(@"M - \d{3}", RegexOptions.IgnorePatternWhitespace)));
    }

    // ---- Collections and uniqueness ---------------------------------------------------------------

    [Fact]
    public void Every_machine_state_once_is_a_set_of_six() {
        Assert.Equal(6, Any.SetOf(Any.Enum<MachineState>()).WithCount(6).Generate().Count);
    }

    [Fact]
    public void Seven_distinct_machine_states_are_refused_by_cardinality() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.SetOf(Any.Enum<MachineState>()).WithCount(7).Generate());

        Assert.Equal("The generator can produce only 6 distinct value(s), not enough for the 7 elements it must still supply.", conflict.Message);
    }

    [Fact]
    public void Distinct_granular_durations_are_gated_by_cardinality() {
        AnyTimeSpan anyWholeMinuteUnderTen = Any.TimeSpan().Between(TimeSpan.Zero, TimeSpan.FromMinutes(9)).WithGranularity(TimeSpan.FromMinutes(1));

        Assert.Equal(10, Any.ListOf(anyWholeMinuteUnderTen).WithCount(10).Distinct().Generate().Count);
        // Observed: refused at Generate(), not at Distinct() — unlike the granularity conflict above.
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.ListOf(anyWholeMinuteUnderTen).WithCount(11).Distinct().Generate());
    }

    [Fact]
    public void All_thousand_machine_ids_can_be_drawn_distinct_but_one_more_exhausts_the_budget() {
        AnyPattern anyMachineId = Any.StringMatching(@"M-\d{3}");

        Assert.Equal(1_000, Any.ListOf(anyMachineId).WithCount(1_000).Distinct().Generate().Count);
        AnyGenerationException exhausted = Assert.Throws<AnyGenerationException>(() => Any.ListOf(anyMachineId).WithCount(1_001).Distinct().Generate());
        Assert.Contains("produced only 1000 distinct value(s)", exhausted.Message);
    }

    // ---- Reproducibility ------------------------------------------------------------------------

    [Fact]
    public void Two_contexts_with_the_same_seed_draw_the_same_values() {
        AnyContext first  = Any.WithSeed(42);
        AnyContext second = Any.WithSeed(42);

        Assert.Equal(first.TimeSpan().Positive().Generate(), second.TimeSpan().Positive().Generate());
        Assert.Equal(first.Decimal().Between(1m, 5_000m).WithScale(3).Generate(), second.Decimal().Between(1m, 5_000m).WithScale(3).Generate());
    }

    [Fact]
    public void A_scaffolded_work_order_replays_under_the_same_seed() {
        string first;
        string second;
        using (IDisposable scope = Any.UseSeed(123)) {
            WorkOrder anyOrder = Any.WorkOrder().Generate();
            first = $"{anyOrder.Batch}/{anyOrder.Window.Start:O}/{anyOrder.Tolerance.NominalMm}/{string.Join(",", anyOrder.Operators)}";
        }
        using (IDisposable scope = Any.UseSeed(123)) {
            WorkOrder anyOrder = Any.WorkOrder().Generate();
            second = $"{anyOrder.Batch}/{anyOrder.Window.Start:O}/{anyOrder.Tolerance.NominalMm}/{string.Join(",", anyOrder.Operators)}";
        }

        Assert.Equal(first, second);
    }

    // The seed below is the one a since-removed test reported on the emitted AnyBatchCode recipe; its first
    // draw is the string the FormatException quoted. The console showed it as "\n3PTKPZUsc[WT),..."
    // because the NUL, form-feed and carriage-return characters it holds are not rendered.
    private const string ReportedDrawStart = "\0\n3PTKPZUs\fc[WT)\f,\fTGjx\r6,.lZ0bFX\0]";
    [Fact]
    public void A_reported_seed_replays_the_failing_draw() {
        using (IDisposable scope = Any.UseSeed(-16025079)) {
            Assert.StartsWith(ReportedDrawStart, Any.String().NotBlank().Generate());
        }
    }

    [Fact, Reproducible(Seed = -16025079)]
    public void The_reported_seed_on_the_attribute_replays_it_too() {
        Assert.StartsWith(ReportedDrawStart, Any.String().NotBlank().Generate());
    }

}
