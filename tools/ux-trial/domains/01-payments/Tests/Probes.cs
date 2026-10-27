using System.Text.RegularExpressions;
using JustDummies;
using Payments.Domain;
using Xunit;

namespace Payments.Tests;

/// <summary>Deliberate edge probes. Each records what the library does; the assertions state what was observed.</summary>
public sealed class Probes {

    private readonly ITestOutputHelper _output;

    public Probes(ITestOutputHelper output) {
        _output = output;
    }

    // ---- contradictions ------------------------------------------------------------------------

    [Fact]
    public void Contradictory_decimal_bounds_fail_fast_naming_both_sides() {
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().GreaterThan(100m).LessThan(10m));
        _output.WriteLine(conflict.Message);

        Assert.Contains("GreaterThan", conflict.Message);
        Assert.Contains("LessThan", conflict.Message);
    }

    [Fact]
    public void A_scale_with_no_point_in_the_range_is_refused() {
        // No two-decimal amount lies strictly between 0.001 and 0.009.
        Exception refusal = Record.Exception(() => Any.Decimal().Between(0.001m, 0.009m).WithScale(2).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void An_integer_lattice_conflict_is_reported() {
        Exception refusal = Record.Exception(() => Any.Int32().Between(1, 10).MultipleOf(50).Generate());
        _output.WriteLine(refusal?.Message);

        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void Contradictory_date_bounds_fail_fast_naming_both_sides() {
        DateOnly cutOff = new(2026, 12, 31);
        ConflictingAnyConstraintException conflict = Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(cutOff).Before(cutOff));
        _output.WriteLine(conflict.Message);

        Assert.Contains("After", conflict.Message);
        Assert.Contains("Before", conflict.Message);
    }

    [Fact]
    public void A_date_pool_emptied_by_a_bound_is_refused_naming_both_sides() {
        DateOnly[] businessDays = [new(2026, 10, 26), new(2026, 10, 27)];
        Exception refusal = Record.Exception(() => Any.DateOnly().OneOf(businessDays).After(new DateOnly(2027, 1, 1)).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void A_currency_enum_emptied_by_exclusions_is_refused() {
        Exception refusal = Record.Exception(() => Any.Enum<Currency>().OneOf(Currency.EUR).DifferentFrom(Currency.EUR).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void An_exclusive_decimal_bound_is_never_drawn() {
        HashSet<decimal> drawn = [];
        for (int i = 0; i < 500; i++) {
            drawn.Add(Any.Decimal().GreaterThan(100m).LessThanOrEqualTo(100.01m).WithScale(2).Generate());
        }

        Assert.Equal([100.01m], drawn);
    }

    [Fact]
    public void An_exclusive_decimal_range_with_no_two_decimal_point_is_refused() {
        Exception refusal = Record.Exception(() => Any.Decimal().GreaterThan(100m).LessThan(100.01m).WithScale(2).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    // ---- bounds and scale ------------------------------------------------------------------------

    [Fact]
    public void A_degenerate_decimal_range_draws_its_single_value() {
        decimal amount = Any.Decimal().Between(42.10m, 42.10m).WithScale(2).Generate();

        Assert.Equal(42.10m, amount);
    }

    [Fact]
    public void WithScale_2_never_draws_more_than_two_decimals() {
        HashSet<byte> scales = [];
        for (int i = 0; i < 2_000; i++) {
            decimal amount = Any.Decimal().Positive().LessThanOrEqualTo(Money.MaximumAmount).WithScale(2).Generate();
            scales.Add(amount.Scale);
            Assert.Equal(decimal.Round(amount, 2), amount);
        }
        _output.WriteLine("scales observed: " + string.Join(", ", scales.Order()));
    }

    [Fact]
    public void WithScale_2_over_the_whole_decimal_range() {
        Exception? refusal = Record.Exception(() => {
            for (int i = 0; i < 500; i++) {
                decimal amount = Any.Decimal().Between(decimal.MinValue, decimal.MaxValue).WithScale(2).Generate();
                Assert.Equal(decimal.Round(amount, 2), amount);
            }
        });
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.Null(refusal);
    }

    [Fact]
    public void A_scale_beyond_decimal_precision_is_refused_as_an_argument_error() {
        Exception refusal = Record.Exception(() => Any.Decimal().WithScale(29));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.IsAssignableFrom<ArgumentException>(refusal);
    }

    [Fact]
    public void Dates_at_the_edges_of_DateOnly() {
        Assert.Equal(DateOnly.MaxValue, Any.DateOnly().AfterOrEqualTo(DateOnly.MaxValue).Generate());
        Assert.Equal(DateOnly.MinValue, Any.DateOnly().BeforeOrEqualTo(DateOnly.MinValue).Generate());

        Exception refusal = Record.Exception(() => Any.DateOnly().After(DateOnly.MaxValue).Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);
        Assert.IsType<ConflictingAnyConstraintException>(refusal);
    }

    [Fact]
    public void TimeOnly_cut_off_times_on_a_quarter_hour_grid() {
        for (int i = 0; i < 200; i++) {
            TimeOnly cutOff = Any.TimeOnly().Between(new TimeOnly(8, 0), new TimeOnly(17, 45)).WithGranularity(TimeSpan.FromMinutes(15)).Generate();
            Assert.Equal(0, cutOff.Minute % 15);
            Assert.InRange(cutOff, new TimeOnly(8, 0), new TimeOnly(17, 45));
        }
    }

    // ---- patterns -----------------------------------------------------------------------------------

    [Fact]
    public void The_iban_format_regex_generates_values_that_match_it() {
        Regex format = new(@"^[A-Z]{2}[0-9]{2}[A-Z0-9]{11,30}$");
        for (int i = 0; i < 500; i++) {
            Assert.Matches(format, Any.StringMatching(format).Generate());
        }
    }

    [Fact]
    public void The_paper_iban_format_with_groups_of_four_generates_matching_values() {
        Regex paper = new(@"^[A-Z]{2}[0-9]{2}( [A-Z0-9]{4}){3,7}( [A-Z0-9]{1,3})?$");
        string sample = Any.StringMatching(paper).Generate();
        _output.WriteLine(sample);

        Assert.Matches(paper, sample);
    }

    [Fact]
    public void Character_class_subtraction_is_honoured_or_refused() {
        // .NET's [A-Z-[AEIOU]] is consonants only. Either refused clearly, or never draws a vowel.
        Exception? refusal = Record.Exception(() => {
            for (int i = 0; i < 200; i++) {
                string drawn = Any.StringMatching("^[A-Z-[AEIOU]]{10}$").Generate();
                Assert.Matches("^[A-Z-[AEIOU]]{10}$", drawn);
            }
        });
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.True(refusal is null or UnsupportedRegexException, refusal?.ToString());
    }

    [Fact]
    public void A_dollar_anchor_never_smuggles_a_trailing_newline() {
        HashSet<int> lengths = [];
        for (int i = 0; i < 2_000; i++) {
            lengths.Add(Any.StringMatching("^[A-Z]{2}$").Generate().Length);
            lengths.Add(Any.StringMatching(@"\A[A-Z]{2}\Z").Generate().Length);
        }
        _output.WriteLine("lengths: " + string.Join(", ", lengths));

        Assert.Equal([2], lengths);
    }

    [Fact]
    public void A_unicode_category_is_refused_or_honoured() {
        Exception? refusal = Record.Exception(() => Assert.Matches(@"^\p{Lu}{4}$", Any.StringMatching(@"^\p{Lu}{4}$").Generate()));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.True(refusal is null or UnsupportedRegexException, refusal?.ToString());
    }

    [Fact]
    public void A_lookahead_used_to_forbid_a_country_is_refused_clearly() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"^(?!XX)[A-Z]{2}[0-9]{2}$"));
        _output.WriteLine(refusal.Message);
    }

    [Fact]
    public void A_lazy_quantifier_is_refused_or_honoured() {
        Exception? refusal = Record.Exception(() => Assert.Matches("^[A-Z]{2,5}?$", Any.StringMatching("^[A-Z]{2,5}?$").Generate()));
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.True(refusal is null or UnsupportedRegexException, refusal?.ToString());
    }

    // ---- reproducibility ------------------------------------------------------------------------------

    [Fact]
    public void The_same_seed_replays_a_whole_scaffolded_payment_batch() {
        PaymentBatch first;
        using (Any.UseSeed(20261027)) {
            first = new AnyPaymentBatch().Generate();
        }
        PaymentBatch second;
        using (Any.UseSeed(20261027)) {
            second = new AnyPaymentBatch().Generate();
        }

        Assert.Equal(first.MessageId, second.MessageId);
        Assert.Equal(first.Transfers.Select(t => t.Creditor.Account.Value + t.Amount + t.RequestedExecutionDate),
                     second.Transfers.Select(t => t.Creditor.Account.Value + t.Amount + t.RequestedExecutionDate));
    }

    [Fact]
    public void A_reported_seed_replays_the_failing_draw() {
        string? report = null;
        string? drawnInFailure = null;
        Assert.Throws<InvalidOperationException>(() => Any.Reproducibly(() => {
            drawnInFailure = new AnyIban().Generate().Value;
            throw new InvalidOperationException("simulated failure");
        }, message => report = message));
        _output.WriteLine(report);

        int seed = int.Parse(Regex.Match(report!, @"seeded with (-?\d+)").Groups[1].Value);
        string? replayed = null;
        Any.Reproducibly(seed, () => replayed = new AnyIban().Generate().Value);

        Assert.Equal(drawnInFailure, replayed);
    }

    [Fact]
    public void A_seeded_context_is_deterministic_for_the_primitives_it_carries() {
        string first  = Any.WithSeed(7).String().AlphaNumeric().WithLength(12).Generate();
        string second = Any.WithSeed(7).String().AlphaNumeric().WithLength(12).Generate();

        Assert.Equal(first, second);
    }

    // ---- collections ----------------------------------------------------------------------------------

    [Fact]
    public void More_distinct_end_to_end_ids_than_the_alphabet_allows_is_refused_by_name() {
        Exception refusal = Record.Exception(() => Any.ListOf(Any.String().AlphaNumeric().WithLength(1)).WithCount(63).Distinct().Generate());
        _output.WriteLine(refusal?.GetType().Name + ": " + refusal?.Message);

        Assert.IsAssignableFrom<DummyException>(refusal);
    }

    [Fact]
    public void Exactly_as_many_distinct_ids_as_the_alphabet_allows_succeeds() {
        List<string> ids = Any.ListOf(Any.String().AlphaNumeric().WithLength(1)).WithCount(62).Distinct().Generate();

        Assert.Equal(62, ids.Distinct().Count());
    }

    [Fact]
    public void A_set_of_currencies_never_holds_more_than_the_enum_declares() {
        HashSet<Currency> currencies = Any.SetOf(Any.Enum<Currency>()).WithCount(6).Generate();

        Assert.Equal(6, currencies.Count);
    }

    // ---- pools ------------------------------------------------------------------------------------------

    [Fact]
    public void Inspecting_a_currency_pool_names_what_a_constraint_removed() {
        string[] catalogue = ["EUR", "CHF", "GBP", "eur", "SEK1"];
        IPoolInspection<string> pool = Any.String().OneOf(catalogue).Alpha().InUpperCase().WithLength(3);

        foreach (PoolRejection<string> rejection in pool.GetRejections()) {
            _output.WriteLine($"{rejection.Value}: {string.Join(", ", rejection.RejectedBy)}");
        }

        Assert.Equal(["EUR", "CHF", "GBP"], pool.GetSurvivors());
    }

}
