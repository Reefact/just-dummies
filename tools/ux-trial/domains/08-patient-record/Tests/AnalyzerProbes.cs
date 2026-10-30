using Clinical.Domain;

using JustDummies;

using Xunit;

namespace Clinical.Tests;

/// <summary>Code an analyzer should catch at build time. Each probe still runs, asserting the run-time refusal.</summary>
public sealed class AnalyzerProbes {

    [Fact]
    public void Constant_crossed_integer_bounds() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.Int32().Between(1, 6).GreaterThan(10));
    }

    [Fact]
    public void Constant_crossed_DateOnly_bounds() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.DateOnly().After(new DateOnly(2026, 1, 1)).Before(new DateOnly(2025, 12, 31)));
    }

    [Fact]
    public void Constant_decimal_range_with_no_point_at_the_scale() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.Decimal().Between(0.0001m, 0.0009m).WithScale(3));
    }

    [Fact]
    public void Nine_distinct_members_of_an_eight_member_enum() {
        Assert.Throws<ConflictingAnyConstraintException>(() => Any.SetOf(Any.Enum<RouteOfAdministration>()).WithCount(9).Generate());
    }

    [Fact]
    public void Eleven_distinct_one_digit_strings() {
        Assert.ThrowsAny<DummyException>(() => Any.ListOf(Any.String().Numeric().WithLength(1)).WithCount(11).Distinct().Generate());
    }

    [Fact]
    public void Constant_unsupported_regex() {
        Assert.Throws<UnsupportedRegexException>(() => Any.StringMatching(@"[12][0-9]{4}(?!00)[0-9]{8}"));
    }

    [Fact]
    public void Distinct_lines_without_value_equality() {
        List<PrescriptionLine> anyLines = Any.ListOf(Any.PrescriptionLine()).WithCount(3).Distinct().Generate();

        Assert.Equal(3, anyLines.Count);
    }

    [Fact]
    public void A_drawn_value_reaching_the_assertion() {
        Nir anyNir = Any.Nir().Generate();

        Nir roundTripped = new Nir(anyNir.Value);

        Assert.Equal(anyNir.Value, roundTripped.Value);
    }

    [Fact]
    public void A_discarded_constraint() {
        AnyInt32 anyIntakes = Any.Int32().Between(1, 6);
        anyIntakes.GreaterThan(2);

        Assert.InRange(anyIntakes.Generate(), 1, 6);
    }

}
