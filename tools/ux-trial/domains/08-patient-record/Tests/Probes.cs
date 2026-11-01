using System.Text.RegularExpressions;

using Clinical.Domain;

using JustDummies;
using JustDummies.Xunit;

using Xunit;

namespace Clinical.Tests;

/// <summary>Deliberate edge cases: each asserts what the library was observed to do.</summary>
[Reproducible]
public sealed class Probes {

    // ---- contradictions: fast, and naming both sides --------------------------------------------

    [Fact]
    public void Intakes_bounded_to_six_then_asked_above_ten_names_both_calls() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Int32().Between(1, 6).GreaterThan(10));

        Assert.Equal("Cannot apply GreaterThan(10) because Between(1, 6) already requires values less than or equal to 6.", conflict.Message);
    }

    [Fact]
    public void A_birth_date_after_2026_and_before_2025_names_both_calls() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.DateOnly().After(new DateOnly(2026, 1, 1)).Before(new DateOnly(2025, 12, 31)));

        Assert.Equal("Cannot apply Before(2025-12-31) because After(2026-01-01) already requires values greater than or equal to 2026-01-02.", conflict.Message);
    }

    [Fact]
    public void A_refinement_contradicting_the_scaffolded_base_names_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Patient().WithBirthDate(b => b.Before(new DateOnly(1890, 1, 1))));

        Assert.Equal("Cannot apply Before(1890-01-01) because Between(1900-01-01, 2025-12-31) already requires values greater than or equal to 1900-01-01.", conflict.Message);
    }

    [Fact]
    public void A_dose_scale_with_no_point_in_range_is_refused() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Decimal().Between(0.0001m, 0.0009m).WithScale(3));

        Assert.Contains("WithScale(3)", conflict.Message);
        Assert.Contains("between 0.0001 and 0.0009", conflict.Message);
    }

    [Fact]
    public void Reversed_Between_on_DateOnly_is_an_argument_error_not_a_conflict() {
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.DateOnly().Between(new DateOnly(2026, 1, 1), new DateOnly(2025, 12, 31)));

        Assert.Equal("start", error.ParamName);
    }

    // ---- bounds ---------------------------------------------------------------------------------

    [Fact]
    public void A_one_day_range_yields_that_day() {
        DateOnly onlyDay = new DateOnly(2026, 1, 1);

        Assert.Equal(onlyDay, Any.DateOnly().Between(onlyDay, onlyDay).Generate());
    }

    [Fact]
    public void After_the_eve_of_MaxValue_is_MaxValue_and_after_MaxValue_is_refused() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().After(DateOnly.MaxValue.AddDays(-1)).Generate());
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(DateOnly.MaxValue));
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().Before(DateOnly.MinValue));
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.TimeOnly().After(TimeOnly.MaxValue));
    }

    [Fact]
    public void A_night_shift_TimeOnly_range_crossing_midnight_is_wrapped() {
        // The start after the end crosses midnight, as TimeOnly.IsBetween reads it (issue #357).
        TimeOnly drawn = Any.TimeOnly().Between(new TimeOnly(20, 0), new TimeOnly(8, 0)).Generate();

        Assert.True(drawn >= new TimeOnly(20, 0) || drawn <= new TimeOnly(8, 0));
    }

    [Fact]
    public void A_scale_beyond_28_is_an_argument_error() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Decimal().Positive().WithScale(29));
    }

    [Fact]
    public void WithScale_2_keeps_two_decimal_places_in_the_ordinary_range() {
        decimal anyAmount = Any.Decimal().Between(0m, 10m).WithScale(2).Generate();

        Assert.Equal(2, anyAmount.Scale);
    }

    [Fact]
    public void WithScale_28_is_a_lattice_not_a_representation() {
        // The user page says WithScale "fixes the number of decimal places"; the XML doc says it is a lattice,
        // not padded. At scale 28 the drawn Decimal.Scale varies (25 to 28 observed); every value is on the grid.
        decimal[] anyRates = Enumerable.Range(0, 200).Select(_ => Any.Decimal().Between(0m, 1m).WithScale(28).Generate()).ToArray();

        Assert.All(anyRates, rate => Assert.Equal(rate, decimal.Round(rate, 28)));
    }

    // ---- regular expressions a NIR or an INN would want -----------------------------------------

    [Fact]
    public void A_lookahead_excluding_department_00_is_refused_by_name_and_position() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"[12][0-9]{4}(?!00)[0-9]{8}"));

        Assert.Contains("negative lookahead '(?!…)' at position 12", refusal.Message);
    }

    [Fact]
    public void Character_class_subtraction_and_Unicode_categories_are_refused_by_name() {
        Assert.Contains("character-class subtraction", Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching("[a-z-[aeiou]]{5}")).Message);
        Assert.Contains("Unicode category", Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"\p{Lu}{3}")).Message);
        Assert.Contains("word-boundary", Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"\bab\b")).Message);
    }

    [Fact]
    public void IgnorePatternWhitespace_is_refused_rather_than_misread() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.StringMatching(new Regex("[0-9] {3}", RegexOptions.IgnorePatternWhitespace)));

        Assert.Contains("IgnorePatternWhitespace", refusal.Message);
    }

    [Fact]
    public void A_digit_shorthand_draws_only_ASCII_digits() {
        string anyDigits = string.Concat(Enumerable.Range(0, 200).Select(_ => Any.StringMatching(@"\d{11}").Generate()));

        Assert.Matches("^[0-9]+$", anyDigits);
    }

    [Fact]
    public void A_formulary_entry_the_INN_pattern_refuses_is_reported_by_the_pool_inspection() {
        IPoolInspection<string> formulary = Any.StringMatching("[a-z][a-z -]{0,79}").OneOf("paracetamol", "Amoxicillin", "insulin glargine");

        PoolRejection<string> rejection = Assert.Single(formulary.GetRejections());

        Assert.Equal("Amoxicillin", rejection.Value);
    }

    // ---- uniqueness -----------------------------------------------------------------------------

    [Fact]
    public void Nine_distinct_routes_out_of_eight_are_refused_up_front() {
        int nine = 9;

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.SetOf(Any.Enum<RouteOfAdministration>()).WithCount(nine).Generate());

        Assert.Equal("The generator can produce only 8 distinct value(s), not enough for the 9 elements it must still supply to honour WithCount(9).", conflict.Message);
    }

    [Fact]
    public void Eleven_distinct_single_digits_exhaust_the_draw_budget_instead_of_being_refused_up_front() {
        int eleven = 11;

        AnyGenerationException exhausted = Assert.Throws<AnyGenerationException>(() => Any.ListOf(Any.String().Numeric().WithLength(1)).WithCount(eleven).Distinct().Generate());

        Assert.Contains("produced only 10 distinct value(s)", exhausted.Message);
    }

    [Fact]
    public void Two_hundred_distinct_NIRs_are_drawn() {
        List<Nir> anyNirs = Any.ListOf(Any.Nir()).WithCount(200).Distinct().Generate();

        Assert.Equal(200, anyNirs.Distinct().Count());
    }

}

/// <summary>Seed probes: no [Reproducible] here, so the explicit scopes are the only ones.</summary>
public sealed class ReproducibilityProbes {

    [Fact]
    public void The_same_seed_replays_the_same_prescription() {
        Guid first  = Guid.Empty;
        Guid second = Guid.Empty;
        string firstNir  = "";
        string secondNir = "";

        Any.Reproducibly(20261027, () => { firstNir  = Any.Nir().Generate().Value; first  = Any.Prescription().Generate().Id; });
        Any.Reproducibly(20261027, () => { secondNir = Any.Nir().Generate().Value; second = Any.Prescription().Generate().Id; });

        Assert.Equal(firstNir, secondNir);
        Assert.Equal(first, second);
    }

    [Fact]
    public void A_failing_body_reports_its_seed_and_that_seed_replays_the_draws() {
        List<string> reports = new List<string>();
        string drawnBeforeFailing = "";

        Assert.Throws<InvalidOperationException>(() => Any.Reproducibly(() => {
            drawnBeforeFailing = Any.Nir().Generate().Value;
            throw new InvalidOperationException("the code under test failed");
        }, reports.Add));

        string report = Assert.Single(reports);
        Match seed = Regex.Match(report, @"seeded with (-?\d+)");
        Assert.True(seed.Success, report);

        string replayed = "";
        Any.Reproducibly(int.Parse(seed.Groups[1].Value), () => replayed = Any.Nir().Generate().Value);
        Assert.Equal(drawnBeforeFailing, replayed);
    }

}
