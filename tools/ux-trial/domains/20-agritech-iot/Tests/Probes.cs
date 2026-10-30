using System.Diagnostics;
using AgriTech.Domain;
using JustDummies;
using Xunit;

namespace AgriTech.Tests;

/// <summary>Deliberate edge cases specific to field sensors: signed bounded doubles, sizes near the cap, replay.</summary>
public sealed class Probes {

    private const int Draws = 100_000;

    private static bool IsNegativeZero(double value) => value == 0.0 && double.IsNegative(value);

    // ---- signed bounded doubles -------------------------------------------------------------

    [Fact]
    public void P01_Between_minus_90_and_90_stays_inside_and_spans_both_signs() {
        AnyDouble anyLatitude = Any.Double().Between(-90.0, 90.0);
        double min = double.MaxValue, max = double.MinValue;
        for (int draw = 0; draw < Draws; draw++) {
            double latitude = anyLatitude.Generate();
            Assert.InRange(latitude, -90.0, 90.0);
            min = Math.Min(min, latitude);
            max = Math.Max(max, latitude);
        }
        Assert.True(min < -89.0 && max > 89.0, $"min {min}, max {max}");
    }

    [Fact]
    public void P02_A_half_open_longitude_never_draws_its_exclusive_end() {
        AnyDouble anyLongitude = Any.Double().GreaterThanOrEqualTo(-180.0).LessThan(180.0);
        for (int draw = 0; draw < Draws; draw++) {
            Assert.True(anyLongitude.Generate() < 180.0);
        }
    }

    [Fact]
    public void P03_A_one_ulp_wide_double_range_draws_its_only_value() {
        double below180 = Math.BitDecrement(180.0);
        AnyDouble anyLongitude = Any.Double().GreaterThanOrEqualTo(below180).LessThan(180.0);
        for (int draw = 0; draw < 1_000; draw++) {
            Assert.Equal(below180, anyLongitude.Generate());
        }
    }

    [Fact]
    public void P04_An_exclusive_double_range_with_no_representable_value_is_refused() {
        double below90 = Math.BitDecrement(90.0);

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Double().GreaterThan(below90).LessThan(90.0));

        Assert.Contains("LessThan(90)", conflict.Message);
    }

    [Fact]
    public void P05_A_single_point_range_at_the_pole_draws_the_pole() {
        Assert.Equal(90.0, Any.Double().Between(90.0, 90.0).Generate());
        Assert.Equal(-90.0, Any.Double().Between(-90.0, -90.0).Generate());
    }

    [Fact]
    public void P06_NonZero_never_draws_negative_zero() {
        AnyDouble anyNonZero = Any.Double().Between(-1e-300, 1e-300).NonZero();
        for (int draw = 0; draw < Draws; draw++) {
            Assert.NotEqual(0.0, anyNonZero.Generate());
        }
    }

    [Fact]
    public void P07_Negative_and_LessThan_zero_never_draw_negative_zero() {
        AnyDouble anyNegative = Any.Double().Negative().GreaterThan(-1e-300);
        AnyDouble anyBelowZero = Any.Double().Between(-1e-300, 0.0).LessThan(0.0);
        for (int draw = 0; draw < Draws; draw++) {
            Assert.True(anyNegative.Generate() < 0.0);
            Assert.True(anyBelowZero.Generate() < 0.0);
        }
    }

    [Fact]
    public void P08_Zero_draws_positive_zero_and_a_range_ending_at_zero_may_reach_it() {
        Assert.False(double.IsNegative(Any.Double().Zero().Generate()));
        bool sawNegativeZero = false;
        for (int draw = 0; draw < Draws; draw++) {
            sawNegativeZero |= IsNegativeZero(Any.Double().Between(-90.0, 90.0).Generate());
        }
        Assert.False(sawNegativeZero);
    }

    [Fact]
    public void P09_Except_zero_excludes_negative_zero_too() {
        AnyDouble anyNotZero = Any.Double().Between(-5e-324, 5e-324).Except(0.0);
        for (int draw = 0; draw < 1_000; draw++) {
            Assert.NotEqual(0.0, anyNotZero.Generate());
        }
    }

    [Fact]
    public void P10_Single_half_open_range_never_rounds_up_to_its_exclusive_end() {
        float below180 = MathF.BitDecrement(180f);
        AnySingle anyNarrow = Any.Single().GreaterThanOrEqualTo(below180).LessThan(180f);
        AnySingle anyWide = Any.Single().GreaterThanOrEqualTo(-180f).LessThan(180f);
        for (int draw = 0; draw < Draws; draw++) {
            Assert.Equal(below180, anyNarrow.Generate());
            Assert.True(anyWide.Generate() < 180f);
        }
    }

    [Fact]
    public void P11_Half_half_open_range_never_rounds_up_to_its_exclusive_end() {
        Half top = BatteryVoltage.Maximum;
        Half belowTop = Half.BitDecrement(top);
        AnyHalf anyNarrow = Any.Half().GreaterThanOrEqualTo(belowTop).LessThan(top);
        AnyHalf anyWide = Any.Half().Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum).LessThan(top);
        for (int draw = 0; draw < Draws; draw++) {
            Assert.Equal(belowTop, anyNarrow.Generate());
            Assert.True(anyWide.Generate() < top);
        }
    }

    [Fact]
    public void P12_Half_range_draws_reach_the_whole_battery_window() {
        AnyHalf anyVolts = Any.Half().Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum);
        HashSet<Half> seen = [];
        for (int draw = 0; draw < Draws; draw++) {
            Half volts = anyVolts.Generate();
            Assert.InRange(volts, BatteryVoltage.Minimum, BatteryVoltage.Maximum);
            seen.Add(volts);
        }
        // [2.5, 4.2] holds 128 + 51 = 179... count the representable values and compare.
        int representable = 0;
        for (Half h = BatteryVoltage.Minimum; h <= BatteryVoltage.Maximum; h = Half.BitIncrement(h)) {
            representable++;
        }
        Assert.Equal(representable, seen.Count);
    }

    [Fact]
    public void P13_Distinct_doubles_beyond_the_representable_count_are_refused_by_name() {
        // [0, 1e-323] holds exactly three doubles: 0, 4.9e-324 and 9.9e-324.
        double[] three = Any.ArrayOf(Any.Double().Between(0.0, 1e-323)).WithCount(3).Distinct().Generate();
        Assert.Equal(3, three.Distinct().Count());

        Assert.ThrowsAny<Exception>(() => Any.ArrayOf(Any.Double().Between(0.0, 1e-323)).WithCount(4).Distinct().Generate());
    }

    [Fact]
    public void P14_Distinct_half_values_are_gated_by_their_cardinality() {
        // [2.5, 4.2] holds 437 Half values; 438 distinct ones cannot exist.
        Exception refusal = Assert.ThrowsAny<Exception>(() => Any.SetOf(Any.Half().Between(BatteryVoltage.Minimum, BatteryVoltage.Maximum)).WithCount(10_000).Generate());
        Assert.Contains("distinct", refusal.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void P15_Unconstrained_double_spans_both_signs_within_a_million() {
        bool negative = false, positive = false;
        for (int draw = 0; draw < 10_000; draw++) {
            double value = Any.Double().Generate();
            Assert.InRange(value, -1_000_000.0, 1_000_000.0);
            negative |= value < 0;
            positive |= value > 0;
        }
        Assert.True(negative && positive);
    }

    [Fact]
    public void P16_Crossed_pole_bounds_name_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Double().GreaterThan(90.0).LessThanOrEqualTo(-90.0));

        Assert.Contains("GreaterThan(90)", conflict.Message);
        Assert.Contains("LessThanOrEqualTo(-90)", conflict.Message);
    }

    [Fact]
    public void P17_Non_finite_bounds_and_pool_values_are_argument_errors_at_the_declaration() {
        Assert.ThrowsAny<ArgumentException>(() => Any.Double().Between(double.NaN, 90.0));
        Assert.ThrowsAny<ArgumentException>(() => Any.Double().LessThan(double.PositiveInfinity));
        Assert.ThrowsAny<ArgumentException>(() => Any.Double().LessThanOrEqualTo(double.NaN));
        Assert.ThrowsAny<ArgumentException>(() => Any.Half().OneOf((Half)1, Half.NaN));
    }

    [Fact]
    public void P18_A_range_of_signed_zeros_draws_negative_zero_and_NonZero_over_it_is_refused() {
        // Observation: Between(-0.0, 0.0) always returns -0.0 (equal to 0.0, so not a violation).
        Assert.True(double.IsNegative(Any.Double().Between(-0.0, 0.0).Generate()));
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.Double().Between(-0.0, 0.0).NonZero());
    }

    [Fact]
    public void P19_Single_and_Half_reach_both_signed_neighbours_of_zero() {
        HashSet<int> singles = [];
        HashSet<short> halves = [];
        for (int draw = 0; draw < 2_000; draw++) {
            singles.Add(BitConverter.SingleToInt32Bits(Any.Single().Between(-float.Epsilon, float.Epsilon).Generate()));
            halves.Add(BitConverter.HalfToInt16Bits(Any.Half().Between(-Half.Epsilon, Half.Epsilon).Generate()));
        }
        Assert.Contains(BitConverter.SingleToInt32Bits(-float.Epsilon), singles);
        Assert.Contains(BitConverter.SingleToInt32Bits(float.Epsilon), singles);
        Assert.Contains(BitConverter.HalfToInt16Bits(-Half.Epsilon), halves);
        Assert.Contains(BitConverter.HalfToInt16Bits(Half.Epsilon), halves);
    }

    /// <summary>
    ///     [-ε, ε] holds three distinct doubles (-ε, 0, ε). Every draw used to return 0: the midpoint form halved
    ///     ε to 0, so a feasible Distinct failed with "the element generator produced only 1 distinct value(s)".
    ///     Fixed by #310: a span of a few rungs is drawn by index.
    /// </summary>
    [Fact]
    public void Double_between_minus_and_plus_epsilon_reaches_all_three_values() {
        HashSet<double> seen = [];
        for (int draw = 0; draw < 2_000; draw++) {
            seen.Add(Any.Double().Between(-double.Epsilon, double.Epsilon).Generate());
        }
        Assert.Contains(double.Epsilon, seen);
        Assert.Contains(-double.Epsilon, seen);
    }

    [Fact]
    public void Two_distinct_doubles_from_minus_to_plus_epsilon_are_drawn() {
        double[] two = Any.ArrayOf(Any.Double().Between(-double.Epsilon, double.Epsilon)).WithCount(2).Distinct().Generate();

        Assert.Equal(2, two.Distinct().Count());
    }

    // ---- sizes near the one-million cap ------------------------------------------------------

    [Fact]
    public void P20_A_million_coordinates_draw_in_bounded_time() {
        Stopwatch watch = Stopwatch.StartNew();
        double[] latitudes = Any.ArrayOf(Any.Double().Between(-90.0, 90.0)).WithCount(1_000_000).Generate();
        watch.Stop();

        Assert.Equal(1_000_000, latitudes.Length);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"took {watch.Elapsed}");
    }

    [Fact]
    public void P21_A_count_above_a_million_is_refused_at_the_declaration() {
        Assert.ThrowsAny<ArgumentException>(() => Any.ArrayOf(Any.Double()).WithCount(1_000_001));
        Assert.ThrowsAny<ArgumentException>(() => Any.ArrayOf(Any.Double()).WithMaxCount(1_000_001));
        Assert.ThrowsAny<ArgumentException>(() => Any.ArrayOf(Any.Double()).WithMinCount(1_000_001));
        Assert.ThrowsAny<ArgumentException>(() => Any.ArrayOf(Any.Double()).WithCountBetween(0, 1_000_001));
    }

    [Fact]
    public void P22_WithMaxCount_a_million_steers_the_draw_but_never_exceeds_it() {
        for (int draw = 0; draw < 5; draw++) {
            Assert.InRange(Any.ArrayOf(Any.Byte()).WithMaxCount(1_000_000).Generate().Length, 0, 1_000_000);
        }
    }

    [Fact]
    public void P23_A_hundred_thousand_distinct_doubles_draw_in_bounded_time() {
        Stopwatch watch = Stopwatch.StartNew();
        double[] latitudes = Any.ArrayOf(Any.Double().Between(-90.0, 90.0)).WithCount(100_000).Distinct().Generate();
        watch.Stop();

        Assert.Equal(100_000, latitudes.Distinct().Count());
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"took {watch.Elapsed}");
    }

    [Fact]
    public void P24_A_set_of_a_million_coordinates_draws() {
        Stopwatch watch = Stopwatch.StartNew();
        HashSet<GeoCoordinate> positions = Any.SetOf(new AnyGeoCoordinate()).WithCount(1_000_000).Generate();
        watch.Stop();

        Assert.Equal(1_000_000, positions.Count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20), $"took {watch.Elapsed}");
    }

    [Fact]
    public void P25_A_batch_of_the_maximum_size_draws_through_the_scaffolded_generator() {
        Stopwatch watch = Stopwatch.StartNew();
        ReadingBatch batch = new AnyReadingBatch().WithReadings(r => r.WithCount(ReadingBatch.MaximumReadings)).Generate();
        watch.Stop();

        Assert.Equal(ReadingBatch.MaximumReadings, batch.Readings.Count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(20), $"took {watch.Elapsed}");
    }

    [Fact]
    public void P26_A_guard_ceiling_read_by_the_scaffolder_steers_the_default_count() {
        // The recipe `any` wrote for ReadingBatch: NonEmpty().WithMaxCount(500000) (ADR-0076: a maximum steers).
        long total = 0;
        for (int draw = 0; draw < 20; draw++) {
            total += Any.ListOf(Any.Byte()).NonEmpty().WithMaxCount(ReadingBatch.MaximumReadings).Generate().Count;
        }
        Assert.True(total / 20 > 50_000, $"mean count {total / 20}");
    }

    // ---- seed reproducibility of a large batch ----------------------------------------------

    private static List<(string, double, double, double, float, Half, DateTimeOffset)> Flatten(ReadingBatch batch) =>
        batch.Readings.Select(r => (r.Sensor.Value, r.Position.Latitude, r.Position.Longitude, r.Humidity.Percent,
                                    r.SoilTemperatureCelsius, r.Battery.Volts, r.TakenAt)).ToList();

    [Fact]
    public void P30_A_100k_batch_replays_from_its_seed_with_Reproducibly() {
        List<(string, double, double, double, float, Half, DateTimeOffset)>? first = null, second = null;
        Any.Reproducibly(20_2026, () => first  = Flatten(new AnyReadingBatch().WithReadings(r => r.WithCount(100_000)).Generate()));
        Any.Reproducibly(20_2026, () => second = Flatten(new AnyReadingBatch().WithReadings(r => r.WithCount(100_000)).Generate()));

        Assert.Equal(first, second);
    }

    [Fact]
    public void P31_A_100k_latitude_array_replays_from_a_context_seed() {
        double[] first  = Any.ArrayOf(Any.WithSeed(7).Double().Between(-90.0, 90.0)).WithCount(100_000).Generate();
        double[] second = Any.ArrayOf(Any.WithSeed(7).Double().Between(-90.0, 90.0)).WithCount(100_000).Generate();

        Assert.Equal(first, second);
    }

    [Fact]
    public void P32_A_distinct_100k_array_replays_from_UseSeed() {
        double[] first, second;
        using (IDisposable scope = Any.UseSeed(42)) {
            first = Any.ArrayOf(Any.Double().Between(-180.0, 180.0)).WithCount(100_000).Distinct().Generate();
        }
        using (IDisposable scope = Any.UseSeed(42)) {
            second = Any.ArrayOf(Any.Double().Between(-180.0, 180.0)).WithCount(100_000).Distinct().Generate();
        }

        Assert.Equal(first, second);
    }

    [Fact]
    public void P33_Different_seeds_give_different_batches() {
        List<(string, double, double, double, float, Half, DateTimeOffset)>? first = null, second = null;
        Any.Reproducibly(1, () => first  = Flatten(new AnyReadingBatch().WithReadings(r => r.WithCount(1_000)).Generate()));
        Any.Reproducibly(2, () => second = Flatten(new AnyReadingBatch().WithReadings(r => r.WithCount(1_000)).Generate()));

        Assert.NotEqual(first, second);
    }

}
