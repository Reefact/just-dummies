using System.Text.RegularExpressions;

using JustDummies;
using JustDummies.Xunit;

using Payroll.Domain;

using Xunit;

namespace Payroll.Tests;

/// <summary>Deliberate edge cases. Values are read from fields so the analyzers leave the run-time checks to speak.</summary>
[Reproducible]
public sealed class Probes {

    private static readonly decimal  Ten          = 10m;
    private static readonly decimal  Five         = 5m;
    private static readonly int      ScaleTooWide = 29;
    private static readonly TimeOnly TenPm        = new TimeOnly(22, 0);
    private static readonly TimeOnly SixAm        = new TimeOnly(6, 0);

    // ---- contradictions -------------------------------------------------------------------------------------

    [Fact]
    public void Crossed_decimal_bounds_are_refused_naming_both() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.Decimal().Between(Ten, Five));

        Assert.Contains("(10)", refusal.Message);
        Assert.Contains("(5)", refusal.Message);
    }

    [Fact]
    public void A_scale_with_no_grid_point_in_range_is_refused_naming_both() {
        decimal tenthOfACent = 0.001m;

        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Decimal().Between(tenthOfACent, 0.009m).WithScale(2));

        Assert.Equal("Cannot apply WithScale(2) because no Decimal value WithScale(2) allows remains between 0.001 and 0.009.", refusal.Message);
    }

    [Fact]
    public void Positive_cents_below_one_cent_are_refused_naming_both() {
        decimal oneCent = 0.01m;

        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.Decimal().Positive().WithScale(2).LessThan(oneCent));

        Assert.Equal("Cannot apply LessThan(0.01) because Positive(), LessThan(0.01) forbid every WithScale(2) value between 0 and 0.01.", refusal.Message);
    }

    [Fact]
    public void Decimal_sign_conflict_message_does_not_claim_zero_is_left() {
        // Int32 says: "Cannot apply Negative() because Positive() already requires values greater than or equal to 1."
        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().Positive().Negative());

        // Before #311: "... Positive() forbids 0, the only value Positive() and Negative() leave." They leave no
        // value at all; 0 is not one they leave.
        Assert.DoesNotContain("the only value Positive() and Negative() leave", refusal.Message);
    }

    [Fact]
    public void A_scale_beyond_what_a_decimal_carries_is_an_argument_error() {
        Assert.Throws<ArgumentOutOfRangeException>("scale", () => Any.Decimal().WithScale(ScaleTooWide));
    }

    [Fact]
    public void A_naive_night_shift_range_is_refused_rather_than_wrapped_round_midnight() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.TimeOnly().Between(TenPm, SixAm));

        Assert.Contains("22:00", refusal.Message);
        Assert.Contains("06:00", refusal.Message);
    }

    [Fact]
    public void A_clock_in_granularity_with_no_point_in_range_is_refused() {
        TimeOnly eightOhOne = new TimeOnly(8, 1);

        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.TimeOnly().Between(eightOhOne, new TimeOnly(8, 14)).WithGranularity(TimeSpan.FromMinutes(15)));

        Assert.Contains("WithGranularity(00:15:00)", refusal.Message);
        Assert.Contains("08:01", refusal.Message);
    }

    // ---- bounds ---------------------------------------------------------------------------------------------

    [Fact]
    public void A_single_cent_range_draws_that_cent() {
        decimal oneCent = 0.01m;

        Assert.Equal(0.01m, Any.Decimal().Between(oneCent, oneCent).WithScale(2).Generate());
    }

    [Fact]
    public void Two_decimal_places_keep_their_trailing_zero() {
        // Observed: WithScale(2) always yields decimal.Scale == 2 (12.50, not 12.5), although the IntelliSense
        // summary says the value "is not padded with trailing zeros".
        for (int draw = 0; draw < 500; draw++) {
            Assert.Equal(2, Any.Decimal().Between(0m, 300m).WithScale(2).Generate().Scale);
        }
    }

    [Fact]
    public void The_full_decimal_range_at_two_places_stays_ordinary() {
        decimal drawn = Any.Decimal().Between(decimal.MinValue, decimal.MaxValue).WithScale(2).Generate();

        Assert.InRange(drawn, -1_000_000m, 1_000_000m);
        Assert.Equal(drawn, decimal.Round(drawn, 2));
    }

    [Fact]
    public void Dates_at_the_edges_of_DateOnly_are_drawn_or_refused() {
        DateOnly lastButOne = DateOnly.MaxValue.AddDays(-1);

        Assert.Equal(DateOnly.MinValue, Any.DateOnly().Between(DateOnly.MinValue, DateOnly.MinValue).Generate());
        Assert.InRange(Any.DateOnly().AfterOrEqualTo(lastButOne).Generate(), lastButOne, DateOnly.MaxValue);
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(DateOnly.MaxValue));
    }

    // ---- patterns -------------------------------------------------------------------------------------------

    [Fact]
    public void A_french_social_security_number_pattern_is_generated_and_matches() {
        const string nir = @"^[12]\d{2}(0[1-9]|1[0-2])(\d{2}|2A|2B)\d{6}\d{2}$";

        string anyNir = Any.StringMatching(nir).Generate();

        Assert.Matches(nir, anyNir);
    }

    [Fact]
    public void A_lookahead_is_refused_naming_the_construct_and_its_position() {
        UnsupportedRegexException refusal = Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"^(?!000)\d{3}$"));

        Assert.Contains("negative lookahead '(?!…)' at position 1", refusal.Message);
    }

    [Fact]
    public void A_pattern_read_with_IgnorePatternWhitespace_is_refused() {
        Regex spaced = new Regex(@"EMP - \d{6}", RegexOptions.IgnorePatternWhitespace);

        Assert.Throws<ArgumentException>("pattern", () => Any.StringMatching(spaced));
    }

    // ---- reproducibility ------------------------------------------------------------------------------------

    internal static string Fingerprint(Payslip payslip) {
        return $"{payslip.Contract.Employee}|{payslip.Contract.Type}|{payslip.Contract.HiredOn}|{payslip.Period.Year}-{payslip.Period.Month}|{payslip.HoursWorked}|{string.Join(",", payslip.Lines.Select(line => line.Label + line.Amount))}";
    }

    [Fact]
    public void The_same_seed_draws_the_same_payslips() {
        List<string> first;
        List<string> second;
        using (Any.UseSeed(20261027)) {
            first = Any.ListOf(Any.Payslip()).WithCount(5).Generate().Select(Fingerprint).ToList();
        }
        using (Any.UseSeed(20261027)) {
            second = Any.ListOf(Any.Payslip()).WithCount(5).Generate().Select(Fingerprint).ToList();
        }

        Assert.Equal(first, second);
    }

    // ---- uniqueness -----------------------------------------------------------------------------------------

    [Fact]
    public void Distinct_contract_types_are_gated_by_how_many_exist() {
        int five = 5;

        ConflictingAnyConstraintException refusal = Assert.Throws<ConflictingAnyConstraintException>(
            () => Any.ListOf(Any.Enum<ContractType>()).WithCount(five).Distinct().Generate());

        Assert.Contains("exceed the 4 distinct value(s)", refusal.Message);
    }

    [Fact]
    public void Every_pay_year_can_be_drawn_once_and_no_more() {
        int everyYear = 101;

        List<int> years = Any.ListOf(Any.Int32().Between(2000, 2100)).WithCount(everyYear).Distinct().Generate();

        Assert.Equal(Enumerable.Range(2000, 101), years.Order());
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.ListOf(Any.Int32().Between(2000, 2100)).WithCount(everyYear + 1).Distinct().Generate());
    }

    [Fact]
    public void Distinct_employee_ids_use_the_value_equality_of_EmployeeId() {
        List<EmployeeId> anyStaff = Any.ListOf(Any.EmployeeId()).WithCount(50).Distinct().Generate();

        Assert.Equal(50, anyStaff.Select(id => id.Value).Distinct().Count());
    }

    // ---- pools ----------------------------------------------------------------------------------------------

    [Fact]
    public void A_rate_catalogue_entry_off_the_cent_grid_is_reported_not_drawn() {
        IPoolInspection<decimal> minimumWages = Any.Decimal().OneOf(11.65m, 11.88m, 12.025m).WithScale(2);

        PoolRejection<decimal> rejection = Assert.Single(minimumWages.GetRejections());

        Assert.Equal(12.025m, rejection.Value);
        Assert.Equal("WithScale", Assert.Single(rejection.RejectedBy).Name);
    }

}

/// <summary>Outside [Reproducible]: JD018 rightly flags an Any.Reproducibly nested in a pinned test.</summary>
public sealed class ReplayProbes {

    [Fact]
    public void A_failing_run_reports_a_seed_that_replays_it() {
        string? report = null;
        string  failingRun = "";

        Assert.Throws<InvalidOperationException>(() => Any.Reproducibly(() => {
            failingRun = Probes.Fingerprint(Any.Payslip().Generate());
            throw new InvalidOperationException("deliberate");
        }, report: message => report = message));

        Assert.NotNull(report);
        int seed = int.Parse(Regex.Match(report, @"seeded with (-?\d+)").Groups[1].Value);
        string replayed = "";
        Any.Reproducibly(seed, () => { replayed = Probes.Fingerprint(Any.Payslip().Generate()); });

        Assert.Equal(failingRun, replayed);
    }

}
