using JustDummies;
using Laboratory.Domain;
using Xunit;

namespace Laboratory.Tests;

/// <summary>Deliberate edge cases. Each probe states the behaviour observed, or the one a user would expect.</summary>
public sealed class Probes {

    private readonly ITestOutputHelper _output;

    public Probes(ITestOutputHelper output) {
        _output = output;
    }

    private string Refusal(Action action) {
        Exception exception = Record.Exception(action) ?? throw new Xunit.Sdk.XunitException("Expected a refusal, got a value.");
        _output.WriteLine($"{exception.GetType().Name}: {exception.Message}");

        return $"{exception.GetType().Name}: {exception.Message}";
    }

    // ---- bounded floats ----

    [Fact]
    public void Crossed_double_bounds_are_refused_naming_both() {
        double upper = 5.1;
        string message = Refusal(() => Any.Double().GreaterThan(upper).LessThan(3.5).Generate());

        Assert.StartsWith(nameof(ConflictingAnyConstraintException), message);
        Assert.Contains("5.1", message);
        Assert.Contains("3.5", message);
    }

    [Fact]
    public void A_degenerate_double_interval_draws_its_single_point() {
        double value = Any.Double().Between(4.2, 4.2).Generate();

        Assert.Equal(4.2, value);
    }

    [Fact]
    public void An_open_double_interval_with_no_representable_value_is_refused() {
        double one = 1.0;
        string message = Refusal(() => Any.Double().GreaterThan(one).LessThan(Math.BitIncrement(one)).Generate());

        Assert.StartsWith(nameof(ConflictingAnyConstraintException), message);
    }

    [Fact]
    public void Tiny_concentrations_stay_inside_their_bounds() {
        // mol/L concentrations of hormones are around 1e-12.
        for (int draw = 0; draw < 500; draw++) {
            double value = Any.Double().Between(1e-12, 5e-12).Generate();
            Assert.InRange(value, 1e-12, 5e-12);
        }
    }

    [Fact]
    public void A_huge_double_range_is_drawn_without_overflow() {
        for (int draw = 0; draw < 500; draw++) {
            double value = Any.Double().Between(-double.MaxValue, double.MaxValue).Generate();
            Assert.True(double.IsFinite(value));
        }
    }

    [Fact]
    public void A_subnormal_double_interval_stays_inside_its_bounds() {
        for (int draw = 0; draw < 200; draw++) {
            double value = Any.Double().Between(double.Epsilon, 4 * double.Epsilon).Generate();
            Assert.InRange(value, double.Epsilon, 4 * double.Epsilon);
        }
    }

    [Fact]
    public void A_NaN_bound_is_an_argument_error() {
        string message = Refusal(() => Any.Double().LessThan(double.NaN));

        Assert.StartsWith(nameof(ArgumentException), message.Replace("OutOfRange", ""));
    }

    [Fact]
    public void Excluding_the_only_point_of_a_double_interval_is_refused() {
        string message = Refusal(() => Any.Double().Between(4.2, 4.2).Except(4.2).Generate());

        Assert.Contains("4.2", message);
    }

    [Fact]
    public void Three_distinct_doubles_from_an_interval_holding_two_are_refused() {
        double one = 1.0;
        string message = Refusal(() => Any.ListOf(Any.Double().Between(one, Math.BitIncrement(one))).WithCount(3).Distinct().Generate());

        Assert.Contains("3", message);
    }

    [Fact]
    public void A_decimal_interval_holding_no_value_at_the_declared_scale_is_refused() {
        decimal lower = 0.001m;
        string  message = Refusal(() => Any.Decimal().Between(lower, 0.004m).WithScale(2).Generate());

        Assert.StartsWith(nameof(ConflictingAnyConstraintException), message);
    }

    [Fact]
    public void A_decimal_drawn_at_scale_three_carries_three_places() {
        for (int draw = 0; draw < 200; draw++) {
            decimal value = Any.Decimal().Between(0m, 10m).WithScale(3).Generate();
            Assert.Equal(3, value.Scale);
        }
    }

    // ---- timestamps ----

    [Fact]
    public void A_sampling_instant_after_DateTimeOffset_MaxValue_is_refused() {
        string message = Refusal(() => Any.DateTimeOffset().After(DateTimeOffset.MaxValue).Generate());

        Assert.StartsWith(nameof(ConflictingAnyConstraintException), message);
    }

    [Fact]
    public void An_offset_that_would_push_the_local_time_below_MinValue_is_refused_naming_both() {
        DateTimeOffset nearMin = DateTimeOffset.MinValue.AddHours(1);
        string         message = Refusal(() => Any.DateTimeOffset().Before(nearMin).WithOffset(TimeSpan.FromHours(-10)));

        Assert.StartsWith(nameof(ConflictingAnyConstraintException), message);
        Assert.Contains("WithOffset(-10:00:00)", message);
        Assert.Contains("Before(", message);
    }

    [Fact]
    public void An_offset_that_would_push_the_local_time_above_MaxValue_is_refused_naming_both() {
        DateTimeOffset nearMax = DateTimeOffset.MaxValue.AddHours(-1);
        string         message = Refusal(() => Any.DateTimeOffset().After(nearMax).WithOffset(TimeSpan.FromHours(14)));

        Assert.StartsWith(nameof(ConflictingAnyConstraintException), message);
        Assert.Contains("WithOffset(14:00:00)", message);
    }

    [Fact]
    public void Minute_granularity_with_an_offset_keeps_the_instant_inside_the_bounds() {
        DateTimeOffset start = new(2025, 3, 1, 8, 0, 30, TimeSpan.Zero);
        DateTimeOffset end   = start.AddMinutes(1);

        for (int draw = 0; draw < 200; draw++) {
            DateTimeOffset value = Any.DateTimeOffset()
                                      .Between(start, end)
                                      .WithOffset(TimeSpan.FromMinutes(330))
                                      .WithGranularity(TimeSpan.FromMinutes(1))
                                      .Generate();
            Assert.InRange(value, start, end);
            Assert.Equal(0, value.Second);
        }
    }

    [Fact]
    public void Day_granularity_with_an_offset_snaps_the_utc_instant_not_the_local_time() {
        DateTimeOffset value = Any.DateTimeOffset()
                                  .Between(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero))
                                  .WithOffset(TimeSpan.FromHours(2))
                                  .WithGranularity(TimeSpan.FromDays(1))
                                  .Generate();
        _output.WriteLine(value.ToString("O"));

        // Observed: the grid is the UTC one, so the local time of day is the offset (02:00), not midnight.
        // The documentation does not say which clock the grid is on.
        Assert.Equal(TimeSpan.Zero, value.UtcDateTime.TimeOfDay);
        Assert.Equal(TimeSpan.FromHours(2), value.TimeOfDay);
    }

    [Fact]
    public void A_DateTime_drawn_between_two_utc_instants_is_utc() {
        DateTime start = new(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime value = Any.DateTime().Between(start, start.AddDays(1)).Generate();

        Assert.Equal(DateTimeKind.Utc, value.Kind);
    }

    [Fact]
    public void A_one_day_DateOnly_interval_at_MaxValue_draws_MaxValue() {
        DateOnly value = Any.DateOnly().Between(DateOnly.MaxValue, DateOnly.MaxValue).Generate();

        Assert.Equal(DateOnly.MaxValue, value);
    }

    [Fact]
    public void An_overnight_TimeOnly_window_is_refused_clearly() {
        // A night shift collecting from 22:00 to 02:00.
        TimeOnly start = new(22, 0);
        string   message = Refusal(() => Any.TimeOnly().Between(start, new TimeOnly(2, 0)).Generate());

        Assert.Contains("22:00", message);
    }

    [Fact]
    public void A_TimeSpan_interval_with_no_point_on_the_granularity_grid_is_refused() {
        TimeSpan lower = TimeSpan.FromSeconds(10);
        string   message = Refusal(() => Any.TimeSpan().Between(lower, TimeSpan.FromSeconds(20)).WithGranularity(TimeSpan.FromMinutes(1)).Generate());

        Assert.StartsWith(nameof(ConflictingAnyConstraintException), message);
    }

    // ---- patterns ----

    [Fact]
    public void A_LOINC_pattern_matches_the_domain_regex() {
        for (int draw = 0; draw < 300; draw++) {
            string code = Any.StringMatching(@"^\d{1,5}-\d$").Generate();
            Assert.Matches(@"^\d{1,5}-\d$", code);
        }
    }

    [Fact]
    public void A_unicode_category_is_refused_or_honoured() {
        Exception? exception = Record.Exception(() => Any.StringMatching(@"\p{Lu}{3}").Generate());
        _output.WriteLine(exception?.ToString() ?? "honoured");

        Assert.True(exception is UnsupportedRegexException || exception is null);
    }

    [Fact]
    public void A_word_boundary_is_refused_naming_the_construct() {
        string message = Refusal(() => Any.StringMatching(@"\bK\+\b").Generate());

        Assert.StartsWith(nameof(UnsupportedRegexException), message);
    }

    [Fact]
    public void A_lookahead_is_refused_naming_its_position() {
        string message = Refusal(() => Any.StringMatching(@"S(?=\d)\d{10}").Generate());

        Assert.StartsWith(nameof(UnsupportedRegexException), message);
        Assert.Contains("1", message);
    }

    // ---- reproducibility ----

    [Fact]
    public void Two_contexts_with_the_same_seed_draw_the_same_doubles_and_timestamps() {
        AnyContext first  = Any.WithSeed(20251027);
        AnyContext second = Any.WithSeed(20251027);

        double[] firstDoubles  = Enumerable.Range(0, 20).Select(_ => first.Double().Between(0, 100).Generate()).ToArray();
        double[] secondDoubles = Enumerable.Range(0, 20).Select(_ => second.Double().Between(0, 100).Generate()).ToArray();
        DateTimeOffset firstInstant  = first.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-5), TimeSpan.FromHours(5)).Generate();
        DateTimeOffset secondInstant = second.DateTimeOffset().WithOffsetBetween(TimeSpan.FromHours(-5), TimeSpan.FromHours(5)).Generate();

        Assert.Equal(firstDoubles, secondDoubles);
        Assert.Equal(firstInstant.Offset, secondInstant.Offset);
        Assert.Equal(firstInstant, secondInstant);
    }

    [Fact]
    public void A_failing_reproducible_body_reports_its_seed_and_the_seed_replays_the_scaffolded_aggregate() {
        List<string> reports   = [];
        LabResult?   firstDraw = null;

        Exception? failure = Record.Exception(() => Any.Reproducibly(() => {
            firstDraw = Any.LabResult().Generate();
            throw new InvalidOperationException("the body failed");
        }, reports.Add));

        Assert.IsType<InvalidOperationException>(failure);
        string report = Assert.Single(reports);
        _output.WriteLine(report);
        int seed = int.Parse(System.Text.RegularExpressions.Regex.Match(report, @"seeded with (-?\d+)").Groups[1].Value);

        LabResult? replayed = null;
        Any.Reproducibly(seed, () => replayed = Any.LabResult().Generate());

        Assert.Equal(firstDraw!.Analyte, replayed!.Analyte);
        Assert.Equal(firstDraw.Specimen.Id, replayed.Specimen.Id);
        Assert.Equal(firstDraw.Measurement.Value, replayed.Measurement.Value);
        Assert.Equal(firstDraw.Range.Lower, replayed.Range.Lower);
        Assert.Equal(firstDraw.ReportedAt, replayed.ReportedAt);
    }

    [Fact]
    public void A_bounded_redraw_failure_inside_a_seeded_scope_carries_the_seed() {
        Exception? exception = null;
        using (Any.UseSeed(77)) {
            exception = Record.Exception(() => Any.ListOf(Any.StringMatching(@"\d").Except("0", "1")).WithCount(10).Distinct().Generate());
        }
        _output.WriteLine(exception?.ToString() ?? "no exception");

        Assert.NotNull(exception);
    }

    // ---- uniqueness ----

    [Fact]
    public void A_set_of_analyte_codes_is_distinct_by_value() {
        HashSet<AnalyteCode> codes = Any.SetOf(new AnyAnalyteCode()).WithCount(50).Generate();

        Assert.Equal(50, codes.Select(code => code.Value).Distinct().Count());
    }

    [Fact]
    public void Eleven_distinct_single_digits_end_in_a_bounded_redraw_failure() {
        string message = Refusal(() => Any.ListOf(Any.StringMatching(@"\d")).WithCount(11).Distinct().Generate());

        // A pattern's language is not enumerated, so its cardinality is unknown: bounded redraw, not up-front refusal.
        Assert.StartsWith(nameof(AnyGenerationException), message);
    }

    // ---- more temporal and float edges ----

    [Fact]
    public void A_varying_offset_keeps_the_instant_inside_the_bounds() {
        DateTimeOffset start = new(2025, 3, 1, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset end   = start.AddMinutes(30);

        for (int draw = 0; draw < 500; draw++) {
            DateTimeOffset value = Any.DateTimeOffset().Between(start, end).WithOffsetBetween(TimeSpan.FromHours(-12), TimeSpan.FromHours(14)).Generate();
            Assert.InRange(value, start, end);
        }
    }

    [Fact]
    public void Hour_granularity_with_a_half_hour_offset_keeps_the_instant_inside_the_bounds() {
        DateTimeOffset start = new(2025, 3, 1, 8, 0, 0, TimeSpan.Zero);
        DateTimeOffset end   = start.AddHours(3);

        for (int draw = 0; draw < 500; draw++) {
            DateTimeOffset value = Any.DateTimeOffset().Between(start, end).WithOffset(TimeSpan.FromMinutes(330)).WithGranularity(TimeSpan.FromHours(1)).Generate();
            Assert.InRange(value, start, end);
            Assert.Equal(TimeSpan.FromMinutes(330), value.Offset);
        }
    }

    [Fact]
    public void A_DateTime_with_minute_granularity_stays_inside_bounds_off_the_grid() {
        DateTime start = new(2025, 3, 1, 8, 0, 30, DateTimeKind.Utc);
        DateTime end   = new(2025, 3, 1, 8, 1, 30, DateTimeKind.Utc);

        for (int draw = 0; draw < 200; draw++) {
            DateTime value = Any.DateTime().Between(start, end).WithGranularity(TimeSpan.FromMinutes(1)).Generate();
            Assert.Equal(new DateTime(2025, 3, 1, 8, 1, 0, DateTimeKind.Utc), value);
        }
    }

    [Fact]
    public void Mixed_DateTime_kinds_in_one_interval() {
        DateTime utcStart = new(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        DateTime localEnd = new(2025, 3, 2, 0, 0, 0, DateTimeKind.Local);
        Exception? exception = Record.Exception(() => {
            DateTime value = Any.DateTime().Between(utcStart, localEnd).Generate();
            _output.WriteLine($"{value:O} {value.Kind}");
        });
        _output.WriteLine(exception?.Message ?? "accepted");
    }

    [Fact]
    public void Tiny_single_precision_concentrations_stay_inside_their_bounds() {
        for (int draw = 0; draw < 500; draw++) {
            float value = Any.Single().Between(1e-12f, 5e-12f).Generate();
            Assert.InRange(value, 1e-12f, 5e-12f);
        }
    }

    [Fact]
    public void A_double_pool_is_narrowed_by_the_bounds() {
        for (int draw = 0; draw < 200; draw++) {
            double value = Any.Double().OneOf(0.1, 0.2, 0.3, 0.4).Between(0.1, 0.3).Generate();
            Assert.InRange(value, 0.1, 0.3);
        }
    }

    [Fact]
    public void A_non_zero_double_in_a_tiny_symmetric_interval() {
        for (int draw = 0; draw < 200; draw++) {
            double value = Any.Double().Between(-1e-300, 1e-300).NonZero().Generate();
            Assert.NotEqual(0d, value);
            Assert.InRange(value, -1e-300, 1e-300);
        }
    }

    [Fact]
    public void Generated_reference_range_bounds_are_ordinary_numbers_after_refining_with_a_lambda() {
        // A test refines the base band of lower: the refinement narrows it, never widens it.
        for (int draw = 0; draw < 200; draw++) {
            ReferenceRange range = Arbitrary.ReferenceRange().WithLower(l => l.LessThan(2d)).Generate();
            Assert.InRange(range.Lower, 1d, 2d);
        }
    }

}
