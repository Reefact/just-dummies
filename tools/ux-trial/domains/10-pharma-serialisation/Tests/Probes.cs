using System.Text.RegularExpressions;
using JustDummies;
using Serialisation.Domain;
using Xunit;

namespace Serialisation.Tests;

/// <summary>Deliberate edge cases of the library, as met while modelling pack serialisation.</summary>
public sealed class Probes {

    private const string ElementStringPattern = @"\(01\)\d{14}\(17\)\d{6}\(10\)[A-Z0-9-]{1,20}\(21\)[A-Za-z0-9]{8,20}";

    // --- contradictions --------------------------------------------------------------------------

    [Fact]
    public void Expiry_after_and_before_the_same_day_fails_fast_naming_both_sides() {
        DateOnly day = new DateOnly(2025, 6, 1);

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(day).Before(day));

        Assert.Contains("After(2025-06-01)", conflict.Message);
        Assert.Contains("Before(2025-06-01)", conflict.Message);
    }

    [Fact]
    public void A_price_to_the_cent_between_two_sub_cent_bounds_is_refused_naming_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().Between(0.001m, 0.009m).WithScale(2));

        Assert.Contains("WithScale(2)", conflict.Message);
        Assert.Contains("0.001", conflict.Message);
    }

    [Fact]
    public void A_positive_price_to_the_cent_below_one_cent_is_refused() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().Positive().WithScale(2).LessThan(0.01m));

        // Observed: "Cannot apply LessThan(0.01) because Positive(), LessThan(0.01) forbid every WithScale(2) value between 0 and 0.01."
        Assert.Contains("Positive()", conflict.Message);
    }

    [Fact]
    public void A_shipment_prefix_longer_than_the_declared_length_is_refused() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.String().StartingWith("SHP-").Numeric().WithLength(int.Parse("3")));

        Assert.Contains("SHP-", conflict.Message);
        Assert.Contains("WithLength(3)", conflict.Message);
    }

    [Fact]
    public void Refining_the_scaffolded_quantity_against_its_base_names_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Arbitrary.Batch().WithPlannedQuantity(q => q.Negative()));

        Assert.Contains("Positive()", conflict.Message);
        Assert.Contains("Negative()", conflict.Message);
    }

    [Fact]
    public void Refining_the_scaffolded_serials_to_empty_names_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Arbitrary.Shipment().WithSerials(l => l.Empty()));

        Assert.Contains("NonEmpty()", conflict.Message);
        Assert.Contains("Empty()", conflict.Message);
    }

    // --- bounds ----------------------------------------------------------------------------------

    [Fact]
    public void Min_equal_to_max_yields_that_value_for_dates_times_and_prices() {
        DateOnly day = new DateOnly(2027, 2, 28);

        Assert.Equal(day, Any.DateOnly().Between(day, day).Generate());
        Assert.Equal(new TimeOnly(8, 0), Any.TimeOnly().Between(new TimeOnly(8, 0), new TimeOnly(8, 0)).Generate());
        Assert.Equal(1m, Any.Decimal().Between(1m, 1m).WithScale(2).Generate());
    }

    [Fact]
    public void Dates_at_the_edges_of_DateOnly_are_handled() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().AfterOrEqualTo(DateOnly.MaxValue).Generate());
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(DateOnly.MaxValue));
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().Before(DateOnly.MinValue));
    }

    [Fact]
    public void A_reversed_date_range_is_an_argument_error_not_a_conflict() {
        // Documented: ordinary argument mistakes stay outside the DummyException hierarchy.
        ArgumentException error = Assert.Throws<ArgumentException>(() => Any.DateOnly().Between(new DateOnly(2025, 6, 1), new DateOnly(2025, 5, 31)));

        Assert.Contains("must be at or before", error.Message);
    }

    [Fact]
    public void A_scale_of_28_works_and_29_is_refused() {
        decimal anyFine = Any.Decimal().Between(0m, 1m).WithScale(28).Generate();

        Assert.InRange(anyFine, 0m, 1m);
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.Decimal().WithScale(int.Parse("29")));
    }

    [Fact]
    public void A_count_above_one_million_is_refused() {
        Assert.Throws<ArgumentOutOfRangeException>(() => Any.ListOf(Any.Int32()).WithCount(int.Parse("1000001")));
    }

    // --- regular expressions ---------------------------------------------------------------------

    [Fact]
    public void The_human_readable_element_string_pattern_is_generated_and_matches() {
        for (int draw = 0; draw < 200; draw++) {
            string anyElementString = Any.StringMatching(ElementStringPattern).Generate();

            Assert.Matches("^" + ElementStringPattern + "$", anyElementString);
        }
    }

    [Fact]
    public void The_GS1_group_separator_escape_is_supported_although_the_escape_table_omits_it() {
        // The FNC1 separator between a variable-length AI and the next is ASCII 29, spelt \x1D.
        string anyRaw = Any.StringMatching(@"21[A-Z]{8}\x1D10[A-Z]{4}").Generate();

        Assert.Matches(@"^21[A-Z]{8}\x1D10[A-Z]{4}$", anyRaw);
        Assert.Contains('\u001D', anyRaw);
    }

    [Fact]
    public void A_lookahead_requiring_a_digit_in_a_serial_is_refused_by_name_and_position() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"(?=.*\d)[A-Za-z0-9]{8,20}"));

        Assert.Contains("lookahead", refusal.Message);
        Assert.Contains("position 0", refusal.Message);
    }

    [Fact]
    public void A_class_subtraction_is_refused_by_name() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"[A-Z-[AEIOU]]{5}"));

        Assert.Contains("subtraction", refusal.Message);
    }

    [Fact]
    public void A_pattern_compiled_with_IgnorePatternWhitespace_is_refused() {
        Assert.Throws<ArgumentException>(() => Any.StringMatching(new Regex(@"\d{3} # three digits", RegexOptions.IgnorePatternWhitespace)));
    }

    // --- uniqueness ------------------------------------------------------------------------------

    [Fact]
    public void A_distinct_list_of_serials_holds_no_repeated_value() {
        List<SerialNumber> anySerials = Any.ListOf(Arbitrary.SerialNumber()).WithCount(500).Distinct().Generate();

        Assert.Equal(500, anySerials.Select(serial => serial.Value).Distinct().Count());
    }

    [Fact]
    public void Eleven_distinct_one_digit_chars_are_refused_before_any_draw() {
        // The cardinality gate runs at Generate(), not at declaration as other conflicts do.
        AnyList<char> elevenDigits = Any.ListOf(Any.Char().Numeric()).WithCount(11).Distinct();

        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => elevenDigits.Generate());

        Assert.Contains("10 distinct", conflict.Message);
    }

    [Fact]
    public void Eleven_distinct_one_digit_strings_are_only_refused_after_the_draw_budget() {
        // Same domain of 10 values as above, but the string generator does not report its cardinality,
        // so the gate is skipped and the bounded redraw ends it at Generate() instead of at declaration.
        AnyList<string> elevenDigits = Any.ListOf(Any.String().Numeric().WithLength(1)).WithCount(11).Distinct();

        AnyGenerationException exhausted = Assert.Throws<AnyGenerationException>(() => elevenDigits.Generate());

        Assert.Contains("only 10 distinct", exhausted.Message);
    }

    [Fact]
    public void Ten_distinct_days_from_a_ten_day_window_take_every_day() {
        DateOnly first = new DateOnly(2026, 1, 1);

        HashSet<DateOnly> anyDays = Any.SetOf(Any.DateOnly().Between(first, first.AddDays(9))).WithCount(10).Generate();

        Assert.Equal(Enumerable.Range(0, 10).Select(first.AddDays).ToHashSet(), anyDays);
    }

    [Fact]
    public void Serials_distinct_ignoring_case_hold_no_case_variant() {
        List<string> anySerials = Any.ListOf(Any.String().AlphaNumeric().WithLength(2)).WithCount(300).Distinct(StringComparer.OrdinalIgnoreCase).Generate();

        Assert.Equal(300, anySerials.Select(serial => serial.ToUpperInvariant()).Distinct().Count());
    }

    [Fact]
    public void A_case_insensitive_distinct_over_a_pool_of_case_variants_is_refused_not_satisfied_wrongly() {
        AnyList<string> threeIgnoringCase = Any.ListOf(Any.String().OneOf("LOT1", "lot1", "LOT2")).WithCount(3).Distinct(StringComparer.OrdinalIgnoreCase);

        Exception refusal = Record.Exception(() => threeIgnoringCase.Generate());

        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    // --- reproducibility -------------------------------------------------------------------------

    [Fact]
    public void The_same_seed_replays_the_same_packs() {
        List<string> first;
        List<string> second;
        using (Any.UseSeed(20261027)) {
            first = Any.ListOf(Arbitrary.SerialisedPack()).WithCount(5).Generate().Select(pack => pack.ToElementString()).ToList();
        }
        using (Any.UseSeed(20261027)) {
            second = Any.ListOf(Arbitrary.SerialisedPack()).WithCount(5).Generate().Select(pack => pack.ToElementString()).ToList();
        }

        Assert.Equal(first, second);
    }

    [Fact]
    public void A_bounded_redraw_failure_inside_a_scope_carries_that_scope_seed() {
        using (Any.UseSeed(424242)) {
            AnyGenerationException exhausted = Assert.Throws<AnyGenerationException>(() => Any.ListOf(Any.String().Numeric().WithLength(1)).WithCount(11).Distinct().Generate());

            Assert.Equal(424242, exhausted.Seed);
            Assert.Contains("Any.Reproducibly(424242, ...)", exhausted.Message);
        }
    }

    [Fact]
    public void A_seed_reported_outside_any_scope_replays_the_draws_of_this_test() {
        // Arrange: draws made with no scope open, then a failure that reports "the" seed.
        string firstSerial  = Arbitrary.SerialNumber().Generate().Value;
        string secondSerial = Arbitrary.SerialNumber().Generate().Value;
        AnyGenerationException exhausted = Assert.Throws<AnyGenerationException>(() => Any.ListOf(Any.String().Numeric().WithLength(1)).WithCount(11).Distinct().Generate());
        Assert.NotNull(exhausted.Seed);

        // Act: replay the same draws with the reported seed.
        List<string> replayed = new List<string>();
        Any.Reproducibly(exhausted.Seed.Value, () => {
            replayed.Add(Arbitrary.SerialNumber().Generate().Value);
            replayed.Add(Arbitrary.SerialNumber().Generate().Value);
        });

        // Assert
        Assert.Equal([firstSerial, secondSerial], replayed);
    }

}
