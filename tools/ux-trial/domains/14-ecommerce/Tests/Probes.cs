using System.Text.RegularExpressions;

using JustDummies;

using Shop.Domain;

using Xunit;

namespace Shop.Tests;

/// <summary>Deliberate edge probes. Each one records the library's actual message to the test output.</summary>
public sealed class Probes {

    private static void Record(string probe, string message) {
        TestContext.Current.TestOutputHelper?.WriteLine($"[{probe}] {message}");
        string? log = Environment.GetEnvironmentVariable("PROBE_LOG");
        if (log is not null) {
            File.AppendAllText(log, $"[{probe}] {message}{Environment.NewLine}");
        }
    }

    private static TException Refused<TException>(string probe, Func<object?> act) where TException : Exception {
        TException refusal = Assert.ThrowsAny<TException>(act);
        Record(probe, $"{refusal.GetType().Name}: {refusal.Message}");

        return refusal;
    }

    // ---- Contradictions ------------------------------------------------------------------------

    [Fact]
    public void Contradictory_discount_bounds_fail_fast_naming_both_sides() {
        decimal ceiling = 100m;
        decimal floor   = 200m;

        ConflictingAnyConstraintException refusal = Refused<ConflictingAnyConstraintException>(
            "decimal-cross", () => Any.Decimal().Between(0m, ceiling).GreaterThan(floor));

        Assert.Contains("200", refusal.Message);
        Assert.Contains("100", refusal.Message);
    }

    [Fact]
    public void A_refinement_contradicting_the_scaffolded_base_names_both_sides() {
        ConflictingAnyConstraintException refusal = Refused<ConflictingAnyConstraintException>(
            "refine-cross", () => Any.DiscountPercentage().WithValue(value => value.GreaterThan(150m)));

        Assert.Contains("150", refusal.Message);
    }

    [Fact]
    public void Emptying_the_scaffolded_non_empty_lines_is_refused() {
        Refused<ConflictingAnyConstraintException>("lines-empty", () => Any.Order().WithLines(lines => lines.Empty()));
    }

    [Fact]
    public void A_scale_with_no_point_in_the_range_is_refused() {
        decimal low  = 0.001m;
        decimal high = 0.009m;

        Refused<DummyException>("scale-gap", () => Any.Decimal().Between(low, high).WithScale(2).Generate());
    }

    // ---- Bound limits --------------------------------------------------------------------------

    [Fact]
    public void A_degenerate_range_draws_its_only_value() {
        decimal only = Any.Decimal().Between(12.5m, 12.5m).WithScale(1).Generate();

        Assert.Equal(12.5m, only);
    }

    [Fact]
    public void The_full_decimal_range_with_cents_draws_values_with_two_places_at_most() {
        for (int draw = 0; draw < 200; draw++) {
            decimal anyAmount = Any.Decimal().Between(decimal.MinValue, decimal.MaxValue).WithScale(2).Generate();
            Assert.Equal(decimal.Round(anyAmount, 2), anyAmount);
        }
    }

    [Fact]
    public void A_scale_of_one_reaches_both_ends_of_a_percentage() {
        HashSet<decimal> seen = [];
        for (int draw = 0; draw < 20_000 && seen.Count < 2; draw++) {
            decimal anyPercent = Any.Decimal().Between(0m, 100m).WithScale(1).Generate();
            Assert.InRange(anyPercent, 0m, 100m);
            if (anyPercent is 0m or 100m) {
                seen.Add(anyPercent);
            }
        }
        Record("percent-ends", $"ends reached: {string.Join(", ", seen)}");
    }

    [Fact]
    public void Dates_next_to_MaxValue_are_drawn_or_refused_cleanly() {
        DateTime last = Any.DateTime().After(DateTime.MaxValue.AddTicks(-1)).Generate();
        Assert.Equal(DateTime.MaxValue, last);

        Refused<DummyException>("after-max", () => Any.DateTime().After(DateTime.MaxValue).Generate());

        DateOnly lastDay = Any.DateOnly().AfterOrEqualTo(DateOnly.MaxValue).Generate();
        Assert.Equal(DateOnly.MaxValue, lastDay);

        TimeOnly firstMinute = Any.TimeOnly().Before(new TimeOnly(0, 1)).WithGranularity(TimeSpan.FromMinutes(1)).Generate();
        Assert.Equal(TimeOnly.MinValue, firstMinute);
    }

    [Fact]
    public void A_count_beyond_a_million_is_refused() {
        int tooMany = 2_000_000;

        Refused<Exception>("count-cap", () => Any.ListOf(Any.Sku()).WithCount(tooMany));
    }

    [Fact]
    public void Between_declared_twice_keeps_the_intersection() {
        decimal min = decimal.MaxValue;
        decimal max = decimal.MinValue;
        for (int draw = 0; draw < 2_000; draw++) {
            decimal anyValue = Any.Decimal().Between(0m, 100m).Between(50m, 200m).Generate();
            min = Math.Min(min, anyValue);
            max = Math.Max(max, anyValue);
        }
        Record("between-twice", $"observed range [{min}, {max}]");
        Assert.InRange(min, 50m, 100m);
        Assert.InRange(max, 50m, 100m);
    }

    // ---- Patterns ------------------------------------------------------------------------------

    [Fact]
    public void The_anchored_production_regex_is_accepted_and_always_matches() {
        Regex production = new Regex(@"^[A-Z]{3}-\d{4}$");
        for (int draw = 0; draw < 200; draw++) {
            Assert.Matches(production, Any.StringMatching(production).Generate());
        }
    }

    [Fact]
    public void A_postal_code_alternation_matches_either_country_format() {
        for (int draw = 0; draw < 200; draw++) {
            Assert.Matches(@"^(\d{4}|\d{5})$", Any.StringMatching(@"(\d{4}|\d{5})").Generate());
        }
    }

    [Fact]
    public void A_unicode_category_is_refused_by_name() {
        Refused<UnsupportedRegexException>("unicode-category", () => Any.StringMatching(@"\p{Lu}{3}-\d{4}"));
    }

    [Fact]
    public void A_lookahead_is_refused_by_name() {
        Refused<UnsupportedRegexException>("lookahead", () => Any.StringMatching(@"(?!ZZZ)[A-Z]{3}-\d{4}"));
    }

    // ---- Reproducibility -----------------------------------------------------------------------

    [Fact]
    public void The_same_seed_draws_the_same_order_twice() {
        Order first;
        Order second;
        using (Any.UseSeed(20261027)) {
            first = Any.Order().Generate();
        }
        using (Any.UseSeed(20261027)) {
            second = Any.Order().Generate();
        }

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Lines.Count, second.Lines.Count);
        Assert.Equal(first.Total, second.Total);
    }

    [Fact]
    public void A_failing_body_reports_a_seed_that_replays_its_draws() {
        string? report = null;
        Guid    failedWith = Guid.Empty;
        Assert.ThrowsAny<Exception>(() => Any.Reproducibly(() => {
            failedWith = Any.Order().Generate().Id;
            throw new InvalidOperationException("simulated failure");
        }, message => report = message));

        Assert.NotNull(report);
        Record("seed-report", report!);
        int seed = int.Parse(Regex.Match(report!, @"seeded with (-?\d+)").Groups[1].Value);

        Guid replayed = Guid.Empty;
        Any.Reproducibly(seed, () => replayed = Any.Order().Generate().Id);

        Assert.Equal(failedWith, replayed);
    }

    // ---- Uniqueness ----------------------------------------------------------------------------

    [Fact]
    public void Distinct_skus_through_a_scaffolded_generator() {
        List<Sku> anySkus = Any.ListOf(Any.Sku()).WithCount(50).Distinct().Generate();

        Assert.Equal(50, anySkus.Distinct().Count());
    }

    [Fact]
    public void More_distinct_shipping_methods_than_exist_is_refused() {
        Refused<DummyException>("set-cardinality", () => Any.SetOf(Any.Enum<ShippingMethod>()).WithCount(4).Generate());
    }

    [Fact]
    public void Distinct_currencies_beyond_the_pool_are_refused() {
        int four = 4;

        Refused<DummyException>("currency-cardinality", () => Any.ListOf(Any.String().OneOf("EUR", "USD", "GBP")).WithCount(four).Distinct().Generate());
    }

}
