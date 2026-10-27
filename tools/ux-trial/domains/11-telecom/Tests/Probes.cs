using System.Text.RegularExpressions;
using JustDummies;
using Telecom.Domain;
using Xunit;

namespace Telecom.Tests;

/// <summary>Deliberate edge cases. Each asserts the behaviour observed, so the file documents the library's answers.</summary>
public sealed class Probes {

    // Read from fields so no analyzer decides the chain at build time: these are the run-time checks.
    private static readonly DateOnly October1   = new DateOnly(2026, 10, 1);
    private static readonly DateOnly September1 = new DateOnly(2026, 9, 1);
    private static readonly decimal  TenthOfACent = 0.001m;

    [Fact]
    public void Crossed_date_bounds_fail_fast_naming_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(October1).Before(September1));

        Assert.Contains("After(2026-10-01)", conflict.Message);
        Assert.Contains("Before(2026-09-01)", conflict.Message);
    }

    [Fact]
    public void A_fee_range_with_no_whole_cent_fails_fast_naming_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().Between(TenthOfACent, 0.009m).WithScale(2));

        Assert.Contains("WithScale(2)", conflict.Message);
        Assert.Contains("0.001", conflict.Message);
    }

    [Fact]
    public void Crossed_Between_bounds_are_an_argument_error_not_a_constraint_conflict() {
        int ten = 10;

        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.Int32().Between(ten, 1));

        Assert.IsNotAssignableFrom<DummyException>(error);
        Assert.Contains("(10)", error.Message);
        Assert.Contains("(1)", error.Message);
    }

    [Fact]
    public void Equal_bounds_yield_that_single_value() {
        Assert.Equal(October1, Any.DateOnly().Between(October1, October1).Generate());
        Assert.Equal(19.99m, Any.Decimal().Between(19.99m, 19.99m).WithScale(2).Generate());
        Assert.Equal(5L, Any.Int64().Between(5, 5).Generate());
    }

    [Fact]
    public void Dates_at_the_edges_of_the_calendar_are_refused_or_honoured_clearly() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().AfterOrEqualTo(DateOnly.MaxValue).Generate());

        ConflictingAnyConstraintException afterMax = Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(DateOnly.MaxValue));
        ConflictingAnyConstraintException afterMidnightEnd = Assert.Throws<ConflictingAnyConstraintException>(() => Any.TimeOnly().After(TimeOnly.MaxValue));

        Assert.Contains("After(9999-12-31)", afterMax.Message);
        Assert.Contains("no TimeOnly value satisfies it", afterMidnightEnd.Message);
    }

    [Fact]
    public void A_drawn_DateTime_is_relabelled_Utc_even_from_local_bounds() {
        // Documented on the type's XML remarks, not on the user page: bounds compare by ticks, the result is Utc.
        DateTime localStart = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Local);

        DateTime drawn = Any.DateTime().Between(localStart, localStart.AddSeconds(1)).Generate();

        Assert.Equal(DateTimeKind.Utc, drawn.Kind);
    }

    [Fact]
    public void Decimal_scale_is_capped_at_28_by_the_type() {
        int twentyNine = 29;

        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Decimal().WithScale(twentyNine));
        // WithScale is a lattice, not a representation: at 28, about one draw in ten comes back with Scale 27 or less
        // (a trailing zero is not kept). The value is on the grid either way.
        decimal anyFraction = Any.Decimal().Between(0m, 1m).WithScale(28).Generate();
        Assert.InRange(anyFraction.Scale, 0, 28);
    }

    [Fact]
    public void Long_numeric_patterns_are_generated_in_ASCII_digits() {
        // .NET's \d also matches Arabic-Indic and other Unicode digits; an IMEI guard checks ASCII.
        for (int draw = 0; draw < 200; draw++) {
            string anyBody = Any.StringMatching(@"\d{15}").Generate();

            Assert.True(anyBody.All(char.IsAsciiDigit), anyBody);
        }
    }

    [Fact]
    public void An_E164_pattern_with_optional_plus_and_anchors_always_matches() {
        Regex e164 = new Regex(@"^\+?[1-9]\d{7,14}$");

        for (int draw = 0; draw < 200; draw++) {
            Assert.Matches(e164, Any.StringMatching(e164).Generate());
        }
    }

    [Fact]
    public void A_lookahead_is_refused_naming_the_construct_and_its_position() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"^(?!0)\d{15}$").Generate());

        Assert.Contains("negative lookahead", refusal.Message);
        Assert.Contains("position 1", refusal.Message);
    }

    [Fact]
    public void Character_class_subtraction_is_refused_by_name() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"[1-9][0-9-[5]]{7}").Generate());

        Assert.Contains("subtraction", refusal.Message);
    }

    [Fact]
    public void A_pattern_beyond_the_generation_limit_is_refused_at_generate() {
        AnyGenerationException refusal = Assert.Throws<AnyGenerationException>(() => Any.StringMatching(@"[0-9]{70000}").Generate());

        Assert.Contains("65536", refusal.Message);
    }

    [Fact]
    public void A_pattern_value_set_it_admits_nothing_of_is_refused_at_declaration() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.StringMatching(@"[1-9][0-9]{7,14}").OneOf("0612345678", "abc"));

        Assert.Contains("OneOf(\"0612345678\", \"abc\")", conflict.Message);
        Assert.Contains("StringMatching(\"[1-9][0-9]{7,14}\")", conflict.Message);
    }

    [Fact]
    public void More_distinct_tiers_than_declared_are_refused_by_cardinality() {
        int four = 4;

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.ListOf(Any.Enum<PlanTier>()).WithCount(four).Distinct().Generate());

        Assert.Contains("3 distinct value(s)", conflict.Message);
    }

    [Fact]
    public void More_distinct_digits_than_a_pattern_has_exhaust_a_bounded_budget() {
        int ten = 10;

        AnyGenerationException exhausted = Assert.Throws<AnyGenerationException>(() => Any.ListOf(Any.StringMatching("[1-9]")).WithCount(ten).Distinct().Generate());

        Assert.Contains("only 9 distinct value(s)", exhausted.Message);
    }

    [Fact]
    public void Distinct_handsets_are_distinct_by_value() {
        List<Imei> anyHandsets = Any.ListOf(Arbitrary.Imei()).WithCount(50).Distinct().Generate();

        Assert.Equal(50, anyHandsets.Select(handset => handset.Value).Distinct().Count());
    }

    [Fact]
    public void The_same_seed_draws_the_same_line_twice() {
        string first  = string.Empty;
        string second = string.Empty;

        Any.Reproducibly(20261027, () => first  = Describe(Arbitrary.MobileLine().Generate()));
        Any.Reproducibly(20261027, () => second = Describe(Arbitrary.MobileLine().Generate()));

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_failing_body_reports_a_seed_that_replays_it_value_for_value() {
        string  failed   = string.Empty;
        string? report   = null;

        Assert.ThrowsAny<Exception>(() => Any.Reproducibly(() => {
                                                               failed = Describe(Arbitrary.MobileLine().Generate());
                                                               throw new InvalidOperationException("the test failed");
                                                           },
                                                           report: message => report = message));

        Match seed = Regex.Match(report!, @"Any\.Reproducibly\((-?\d+),");
        string replayed = string.Empty;
        Any.Reproducibly(int.Parse(seed.Groups[1].Value), () => replayed = Describe(Arbitrary.MobileLine().Generate()));

        Assert.Equal(failed, replayed);
    }

    [Fact]
    public void A_catalogue_of_plan_names_can_be_inspected_for_names_the_invariant_drops() {
        string[] catalogue = ["Essentiel 5 Go", "Illimité", "Forfait Découverte avec appels illimités vers 120 destinations", "Pro"];

        IPoolInspection<string> pool = Any.String().OneOf(catalogue).NotBlank().WithMaxLength(40);

        PoolRejection<string> rejection = Assert.Single(pool.GetRejections());
        Assert.StartsWith("Forfait Découverte", rejection.Value);
        Assert.Equal("WithMaxLength", rejection.RejectedBy[0].Name);
    }

    private static string Describe(MobileLine line) {
        return $"{line.Number}|{line.Handset.Value}|{line.Plan.Name}|{line.Plan.Tier}|{line.Plan.MonthlyFee}|{line.Plan.DataAllowanceGb}|{line.Plan.OveragePerGb}|{line.ActivatedOn}";
    }

}
