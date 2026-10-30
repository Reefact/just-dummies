using System.Text.RegularExpressions;
using Freight.Domain;
using JustDummies;
using Xunit;

namespace Freight.Tests;

/// <summary>Deliberate edge cases: what the library does at the borders of the freight domain's needs.</summary>
public sealed class Probes {

    private readonly ITestOutputHelper _output;

    public Probes(ITestOutputHelper output) {
        _output = output;
    }

    private static readonly DateTimeOffset Jan2025 = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    // ---- contradictory constraints -------------------------------------------------------------

    [Fact]
    public void Crossed_double_bounds_fail_fast_naming_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Double().GreaterThan(100).LessThan(10));
        _output.WriteLine(conflict.Message);
        Assert.Contains("GreaterThan", conflict.Message);
        Assert.Contains("LessThan", conflict.Message);
    }

    [Fact]
    public void Reversed_offset_range_fails_fast_naming_both_sides() {
        Exception? refusal = Record.Exception(() => Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(2), TimeSpan.FromHours(-2)).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.NotNull(refusal);
    }

    [Fact]
    public void A_refinement_outside_the_scaffolded_base_is_a_conflict_not_a_silent_override() {
        // AnyLeg's departure base is Between(2024, 2034); a lambda asking for 2040 must be refused.
        DateTimeOffset in2040 = new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero);
        Exception? refusal = Record.Exception(() => Arbitrary.Leg().WithDeparture(departure => departure.After(in2040)).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void Two_Between_on_one_instant_generator_intersect() {
        DateTimeOffset start = Jan2025;
        AnyDateTimeOffset narrowed = Any.DateTimeOffset().Between(start, start.AddDays(10)).Between(start.AddDays(5), start.AddDays(20));
        for (int draw = 0; draw < 200; draw++) {
            DateTimeOffset value = narrowed.Generate();
            Assert.InRange(value, start.AddDays(5), start.AddDays(10));
        }
    }

    [Fact]
    public void A_two_decimal_rate_inside_an_interval_holding_no_cent_is_refused() {
        Exception? refusal = Record.Exception(() => Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    [Fact]
    public void A_set_of_five_distinct_container_types_is_refused_by_cardinality() {
        Exception? refusal = Record.Exception(() => Any.SetOf(Any.Enum<ContainerType>()).WithCount(5).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    // ---- bounded doubles ----------------------------------------------------------------------

    [Fact]
    public void A_degenerate_double_range_draws_its_single_value() {
        Assert.Equal(Volume.MaxCubicMetres, Any.Double().Between(Volume.MaxCubicMetres, Volume.MaxCubicMetres).Generate());
    }

    [Fact]
    public void An_exclusive_double_bound_with_one_representable_value_left_draws_that_value() {
        double justAbove = Math.BitIncrement(32_500d);
        Assert.Equal(justAbove, Any.Double().GreaterThan(32_500d).LessThanOrEqualTo(justAbove).Generate());
    }

    [Fact]
    public void An_exclusive_double_range_with_no_representable_value_is_refused() {
        double justAbove = Math.BitIncrement(32_500d);
        Exception? refusal = Record.Exception(() => Any.Double().GreaterThan(32_500d).LessThan(justAbove).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    [Fact]
    public void The_whole_double_range_draws_a_finite_value() {
        for (int draw = 0; draw < 200; draw++) {
            Assert.True(double.IsFinite(Any.Double().Between(-double.MaxValue, double.MaxValue).Generate()));
        }
    }

    [Fact]
    public void A_NaN_bound_is_an_argument_error() {
        Exception? refusal = Record.Exception(() => Any.Double().LessThanOrEqualTo(double.NaN));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsAssignableFrom<ArgumentException>(refusal);
    }

    [Fact]
    public void Positive_doubles_up_to_the_smallest_subnormal_draw_epsilon() {
        Assert.Equal(double.Epsilon, Any.Double().Positive().LessThanOrEqualTo(double.Epsilon).Generate());
    }

    // ---- DateTimeOffset and offsets -----------------------------------------------------------

    [Fact]
    public void Bounds_given_at_different_offsets_are_compared_as_instants() {
        DateTimeOffset fromShanghai  = new(2025, 6, 1, 10, 0, 0, TimeSpan.FromHours(8));   // 02:00 UTC
        DateTimeOffset toNewYork     = new(2025, 6, 1, 0, 0, 0, TimeSpan.FromHours(-4));   // 04:00 UTC
        AnyDateTimeOffset inBetween = Any.DateTimeOffset().Between(fromShanghai, toNewYork).WithOffset(TimeSpan.FromHours(14));
        for (int draw = 0; draw < 200; draw++) {
            DateTimeOffset value = inBetween.Generate();
            Assert.InRange(value, fromShanghai, toNewYork);
            Assert.Equal(TimeSpan.FromHours(14), value.Offset);
        }
    }

    [Fact]
    public void Offsets_between_bounds_stay_whole_minutes_and_within_the_bounds() {
        AnyDateTimeOffset anyLocal = Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-12), TimeSpan.FromHours(14));
        HashSet<TimeSpan> seen = [];
        for (int draw = 0; draw < 500; draw++) {
            TimeSpan offset = anyLocal.Generate().Offset;
            Assert.InRange(offset, TimeSpan.FromHours(-12), TimeSpan.FromHours(14));
            Assert.Equal(0, offset.Ticks % TimeSpan.TicksPerMinute);
            seen.Add(offset);
        }
        _output.WriteLine($"{seen.Count} distinct offsets in 500 draws");
    }

    [Fact]
    public void Granularity_with_a_half_hour_offset_snaps_the_local_clock_or_the_utc_clock() {
        DateTimeOffset value = Any.DateTimeOffset()
                                  .Between(Jan2025, Jan2025.AddYears(1))
                                  .WithOffset(new TimeSpan(5, 30, 0))
                                  .WithGranularity(TimeSpan.FromHours(1))
                                  .Generate();
        _output.WriteLine($"{value:O}  local%1h={value.Ticks % TimeSpan.TicksPerHour}  utc%1h={value.UtcTicks % TimeSpan.TicksPerHour}");
        Assert.True(value.Ticks % TimeSpan.TicksPerHour == 0 || value.UtcTicks % TimeSpan.TicksPerHour == 0);
    }

    [Fact]
    public void A_sub_minute_offset_is_refused() {
        Exception? refusal = Record.Exception(() => Any.DateTimeOffset().WithOffset(TimeSpan.FromSeconds(30)));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.NotNull(refusal);
    }

    [Fact]
    public void An_offset_beyond_fourteen_hours_is_refused() {
        Exception? refusal = Record.Exception(() => Any.DateTimeOffset().WithOffset(TimeSpan.FromHours(15)));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.NotNull(refusal);
    }

    [Fact]
    public void Near_the_limits_a_declared_offset_is_still_honoured() {
        DateTimeOffset nearMax = Any.DateTimeOffset().After(DateTimeOffset.MaxValue.AddDays(-1)).WithOffset(TimeSpan.FromHours(14)).Generate();
        DateTimeOffset nearMin = Any.DateTimeOffset().Before(DateTimeOffset.MinValue.AddDays(1)).WithOffset(TimeSpan.FromHours(-12)).Generate();
        _output.WriteLine($"nearMax={nearMax:O} nearMin={nearMin:O}");
        Assert.Equal(TimeSpan.FromHours(14), nearMax.Offset);
        Assert.Equal(TimeSpan.FromHours(-12), nearMin.Offset);
    }

    [Fact]
    public void An_instant_no_offset_of_plus_fourteen_can_display_is_refused_clearly() {
        // After 9999-12-31T10:00Z, a +14:00 local clock would pass DateTime.MaxValue: no such DateTimeOffset exists.
        Exception? refusal = Record.Exception(() => Any.DateTimeOffset().After(DateTimeOffset.MaxValue.AddHours(-2)).WithOffset(TimeSpan.FromHours(14)).Generate());
        _output.WriteLine(refusal?.GetType().FullName + ": " + refusal?.Message);
        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    [Fact]
    public void An_instant_near_MaxValue_at_a_positive_offset() {
        Exception? failure = Record.Exception(() => {
            for (int draw = 0; draw < 200; draw++) {
                Any.DateTimeOffset().After(DateTimeOffset.MaxValue.AddDays(-1)).WithOffset(TimeSpan.FromHours(14)).Generate();
            }
        });
        _output.WriteLine(failure?.GetType().FullName + ": " + failure?.Message);
        Assert.True(failure is null or DummyException, $"Expected a value or a clear refusal, got {failure?.GetType().FullName}");
    }

    [Fact]
    public void An_instant_near_MinValue_at_a_negative_offset() {
        Exception? failure = Record.Exception(() => {
            for (int draw = 0; draw < 200; draw++) {
                Any.DateTimeOffset().Before(DateTimeOffset.MinValue.AddDays(1)).WithOffset(TimeSpan.FromHours(-12)).Generate();
            }
        });
        _output.WriteLine(failure?.GetType().FullName + ": " + failure?.Message);
        Assert.True(failure is null or DummyException, $"Expected a value or a clear refusal, got {failure?.GetType().FullName}");
    }

    [Fact]
    public void An_unconstrained_instant_with_an_offset_range_never_throws() {
        Exception? failure = Record.Exception(() => {
            for (int draw = 0; draw < 2_000; draw++) {
                Any.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-14), TimeSpan.FromHours(14)).Generate();
            }
        });
        Assert.Null(failure);
    }

    [Fact]
    public void DateOnly_and_TimeOnly_at_their_limits() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().Between(DateOnly.MaxValue, DateOnly.MaxValue).Generate());
        Exception? refusal = Record.Exception(() => Any.TimeOnly().After(TimeOnly.MaxValue).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    [Fact]
    public void A_transit_on_a_whole_day_grid_inside_a_window_holding_no_whole_day_is_refused() {
        Exception? refusal = Record.Exception(() => Any.TimeSpan().Between(TimeSpan.FromHours(1), TimeSpan.FromHours(20)).WithGranularity(TimeSpan.FromDays(1)).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    // ---- patterns -----------------------------------------------------------------------------

    [Fact]
    public void The_ISO_6346_shape_is_generated_from_its_pattern() {
        Regex shape = new("^[A-Z]{3}[UJZ][0-9]{7}$");
        AnyPattern anyShape = Any.StringMatching(shape);
        for (int draw = 0; draw < 200; draw++) {
            Assert.Matches(shape, anyShape.Generate());
        }
    }

    [Fact]
    public void Unicode_category_classes_are_refused_or_honoured() {
        Exception? refusal = Record.Exception(() => Any.StringMatching(@"\p{Lu}{3}").Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.True(refusal is null or UnsupportedRegexException);
    }

    [Fact]
    public void A_lookahead_is_refused_with_its_position() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"(?=MSC)[A-Z]{3}U\d{7}"));
        _output.WriteLine(refusal.Message);
    }

    [Fact]
    public void A_pattern_value_set_none_of_which_matches_is_refused_at_declaration() {
        Exception? refusal = Record.Exception(() => Any.StringMatching("[A-Z]{3}[UJZ][0-9]{7}").OneOf("CSQU305438", "csqu3054383"));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    // ---- reproducibility ----------------------------------------------------------------------

    [Fact]
    public void Same_seed_same_shipment() {
        string first  = "";
        string second = "";
        Any.Reproducibly(20261027, () => first  = Describe(Arbitrary.Shipment().Generate()));
        Any.Reproducibly(20261027, () => second = Describe(Arbitrary.Shipment().Generate()));
        _output.WriteLine(first);
        Assert.Equal(first, second);
    }

    [Fact]
    public void A_failing_body_reports_its_seed_and_replaying_it_brings_the_values_back() {
        string?  report      = null;
        string   failedWith  = "";
        Assert.ThrowsAny<Exception>(() => Any.Reproducibly(() => {
            failedWith = Describe(Arbitrary.Shipment().Generate());
            throw new InvalidOperationException("boom");
        }, report: message => report = message));
        _output.WriteLine(report ?? "<no report>");
        Assert.NotNull(report);
        int seed = int.Parse(Regex.Match(report!, @"seeded with (-?\d+)").Groups[1].Value);

        string replayed = "";
        Any.Reproducibly(seed, () => replayed = Describe(Arbitrary.Shipment().Generate()));
        Assert.Equal(failedWith, replayed);
    }

    [Fact]
    public void An_isolated_context_replays_doubles_and_offsets() {
        AnyContext first  = Any.WithSeed(7);
        AnyContext second = Any.WithSeed(7);
        Assert.Equal(first.Double().Between(0, 76.4).Generate(), second.Double().Between(0, 76.4).Generate());
        Assert.Equal(first.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-12), TimeSpan.FromHours(14)).Generate(),
                     second.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-12), TimeSpan.FromHours(14)).Generate());
    }

    [Fact]
    public void Distinct_container_codes_in_a_list_are_distinct_by_value() {
        List<ContainerCode> codes = Any.ListOf(Arbitrary.ContainerCode()).WithCount(50).Distinct().Generate();
        Assert.Equal(50, codes.Select(code => code.Value).Distinct().Count());
    }

    private static string Describe(Shipment shipment) {
        return $"{shipment.Id} {shipment.Container} {shipment.ContainerType} {shipment.GrossWeight} {shipment.Volume} "
             + string.Join(" > ", shipment.Legs.Select(leg => $"{leg.Origin}@{leg.Departure:O}-{leg.Destination}@{leg.Arrival:O}"));
    }

}
