using System.Text.RegularExpressions;

using Insurance.Domain;

using JustDummies;

using Xunit;

namespace Insurance.Tests;

/// <summary>Deliberate edge cases. Each passing probe documents behaviour observed; each Bug_ probe fails on purpose.</summary>
public sealed class Probes {

    // ---- contradictory constraints -------------------------------------------------------------

    [Fact]
    public void Crossed_coefficient_bounds_fail_fast_naming_both_sides() {
        double ceiling = BonusMalus.Ceiling;
        double floor   = BonusMalus.Floor;

        ConflictingAnyConstraintException conflict =
            Assert.Throws<ConflictingAnyConstraintException>(() => Any.Double().GreaterThan(ceiling).LessThan(floor));

        Assert.Contains("GreaterThan(3.5)", conflict.Message);
        Assert.Contains("LessThan(0.5)", conflict.Message);
    }

    [Fact]
    public void Crossed_period_dates_fail_fast_naming_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.DateOnly().After(new DateOnly(2027, 1, 1)).Before(new DateOnly(2026, 1, 1)));

        Assert.Contains("After(2027-01-01)", conflict.Message);
        Assert.Contains("Before(2026-01-01)", conflict.Message);
    }

    [Fact]
    public void A_refinement_contradicting_a_scaffolded_base_is_refused_naming_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.CoveragePeriod().WithLengthInDays(days => days.GreaterThan(400)));

        Assert.Contains("GreaterThan(400)", conflict.Message);
        Assert.Contains("Between(1, 365)", conflict.Message);
    }

    [Fact]
    public void Negative_on_a_scaled_deductible_range_names_Between_as_the_other_side() {
        // Before #312 the message read "Cannot apply Negative() because it forbids every WithScale(2) value
        // between 0 and 0.", which never named Between(0, 5000), the other side of the conflict.
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Deductible().WithAmount(amount => amount.Negative()));

        Assert.Contains("Between(0, 5000)", conflict.Message);
    }

    // ---- bounds ------------------------------------------------------------------------------------

    [Fact]
    public void Min_equal_to_max_yields_that_value() {
        Assert.Equal(0.50, Any.Double().Between(0.50, 0.50).Generate());
        Assert.Equal(new DateOnly(2026, 1, 1), Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1)).Generate());
    }

    [Fact]
    public void The_last_day_of_the_calendar_is_reachable_and_the_day_after_is_refused() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().AfterOrEqualTo(DateOnly.MaxValue).Generate());
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(DateOnly.MaxValue));
    }

    [Fact]
    public void A_scale_with_no_value_in_range_is_refused() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Decimal().Between(0.001m, 0.009m).WithScale(2));

        Assert.Contains("WithScale(2)", conflict.Message);
    }

    [Fact]
    public void WithScale_2_near_decimal_MaxValue_stays_on_the_lattice_without_trailing_zeros() {
        // First reported as a bug: values such as 7900000000000000000001454003.8 carry scale 1. Verification showed
        // WithScale is a value lattice (ADR-0015), not a representation contract: the value is a multiple of 0.01,
        // and a decimal this large has no room left for the trailing zero. The user page is what misleads.
        decimal anyHuge = Any.Decimal().GreaterThanOrEqualTo(7.9e27m).WithScale(2).Generate();

        Assert.Equal(0m, anyHuge % 0.01m);
    }

    [Fact]
    public void An_overnight_time_window_is_refused_as_reversed_bounds() {
        // A night-driving window (22:00 to 02:00) cannot be declared: Between demands start <= end.
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => Any.TimeOnly().Between(new TimeOnly(22, 0), new TimeOnly(2, 0)));

        Assert.Contains("must be at or before the end", refusal.Message);
    }

    [Fact]
    public void A_granularity_with_no_point_in_range_is_refused() {
        Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.TimeOnly().Between(new TimeOnly(8, 5), new TimeOnly(8, 10)).WithGranularity(TimeSpan.FromMinutes(15)));
    }

    // ---- patterns ----------------------------------------------------------------------------------

    [Fact]
    public void The_policy_number_pattern_always_satisfies_the_domain_regex() {
        Regex domainFormat = new(@"^POL-\d{4}-[A-Z]{2}\d{6}$");
        AnyPattern anyNumber = Any.StringMatching(@"^POL-\d{4}-[A-Z]{2}\d{6}$");

        for (int draw = 0; draw < 500; draw++) {
            Assert.Matches(domainFormat, anyNumber.Generate());
        }
    }

    [Theory]
    [InlineData(@"POL-\d{4}-\p{Lu}{2}\d{6}", "Unicode category")]
    [InlineData(@"POL-\d{4}-[A-Z-[IOQ]]{2}\d{6}", "character-class subtraction")]
    [InlineData(@"(?!POL-0000)POL-\d{4}", "negative lookahead")]
    [InlineData(@"(?i:pol)-\d{4}", "group option")]
    public void Unsupported_regex_constructs_are_refused_by_name(string pattern, string construct) {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(pattern));

        Assert.Contains(construct, refusal.Message);
    }

    // ---- collections -------------------------------------------------------------------------------

    [Fact]
    public void Asking_for_more_distinct_responsibilities_than_exist_is_refused() {
        int four = int.Parse("4");

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.SetOf(Any.Enum<Responsibility>()).WithCount(four).Generate());

        Assert.Contains("only 3 distinct", conflict.Message);
    }

    [Fact]
    public void Distinct_claim_dates_within_a_short_period_are_gated_by_cardinality() {
        DateOnly start = new(2026, 1, 1);
        int      four  = int.Parse("4");

        Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.ListOf(Any.DateOnly().Between(start, start.AddDays(2))).WithCount(four).Distinct().Generate());
    }

    [Fact]
    public void A_list_containing_one_at_fault_claim_holds_one() {
        List<Claim> anyClaims = Any.ListOf(Any.Claim())
                                   .WithCountBetween(1, 5)
                                   .ContainingAny(Any.Claim().WithResponsibility(Responsibility.Full))
                                   .Generate();

        Assert.Contains(anyClaims, claim => claim.Responsibility == Responsibility.Full);
    }

    // ---- pools -------------------------------------------------------------------------------------

    [Fact]
    public void Inspecting_a_country_catalogue_reports_codes_the_policy_format_refuses() {
        string[] countries = ["FR", "BE", "fr", "GBR", "DE"];

        IPoolInspection<string> pool = Any.String().OneOf(countries).Alpha().InUpperCase().WithLength(2);

        Assert.Equal(["FR", "BE", "DE"], pool.GetSurvivors());
        Assert.Equal(["fr", "GBR"], pool.GetRejections().Select(rejection => rejection.Value));
    }

    // ---- reproducibility ---------------------------------------------------------------------------

    private static string Fingerprint(CarPolicy policy) {
        return string.Join("|", policy.Number, policy.Driver.FullName, policy.Driver.BirthDate, policy.Vehicle, policy.Level,
                           policy.Period.Start, policy.Period.End, policy.Deductible.Amount, policy.BonusMalus.Coefficient,
                           policy.BasePremium, string.Join(",", policy.Claims.Select(claim => $"{claim.Id}@{claim.OccurredOn}")));
    }

    [Fact]
    public void The_same_seed_replays_a_whole_policy() {
        string first  = "";
        string second = "";

        Any.Reproducibly(20261027, () => first  = Fingerprint(Any.CarPolicy().Generate()));
        Any.Reproducibly(20261027, () => second = Fingerprint(Any.CarPolicy().Generate()));

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_failing_run_reports_a_seed_that_replays_it() {
        string       drawn    = "";
        List<string> reported = [];

        Assert.ThrowsAny<Exception>(() => Any.Reproducibly(() => {
            drawn = Fingerprint(Any.CarPolicy().Generate());
            throw new InvalidOperationException("the test failed");
        }, report: reported.Add));

        Match seed = Regex.Match(Assert.Single(reported), @"seeded with (-?\d+)");
        Assert.True(seed.Success, reported[0]);

        string replayed = "";
        Any.Reproducibly(int.Parse(seed.Groups[1].Value), () => replayed = Fingerprint(Any.CarPolicy().Generate()));

        Assert.Equal(drawn, replayed);
    }

    [Fact]
    public void The_adapter_scope_and_Reproducibly_draw_the_same_values_for_the_same_seed() {
        string fromReproducibly = "";
        Any.Reproducibly(777, () => fromReproducibly = Fingerprint(Any.CarPolicy().Generate()));

        string fromScope;
        using (IDisposable scope = Any.UseSeed(777, "[Reproducible(Seed = 777)]")) {
            fromScope = Fingerprint(Any.CarPolicy().Generate());
        }

        Assert.Equal(fromReproducibly, fromScope);
    }

}
