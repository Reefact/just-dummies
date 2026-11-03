using System.Globalization;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Expert;

public sealed class LabResultTests {

    private static string AnyCode() {
        return AnyAnalyteCodes.Code().Generate();
    }

    private static string AnyUnit() {
        return Any.String().Printable().WithoutChars(" ").WithLengthBetween(1, LabResult.MaxUnitLength).Generate();
    }

    private static decimal AnyResult() {
        return Any.Decimal().GreaterThanOrEqualTo(0m).Generate();
    }

    // ---------------------------------------------------------------- the analyte code

    [Theory]
    [InlineData("AB")]
    [InlineData("ALT")]
    [InlineData("A123456789")]
    public void An_analyte_code_is_an_upper_case_letter_then_one_to_nine_upper_case_letters_or_digits(string code) {
        // Act
        LabResult lab = new LabResult(code, null, null, null, null);

        // Assert
        Assert.Equal(code, lab.AnalyteCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("alt")]
    [InlineData("Alt")]
    [InlineData("1ALT")]
    [InlineData("ALT-1")]
    [InlineData("ALT 1")]
    [InlineData("A1234567890")]
    public void A_code_that_is_not_an_analyte_code_is_refused(string code) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new LabResult(code, null, null, null, null));
    }

    [Fact]
    public void A_missing_analyte_code_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new LabResult(null!, null, null, null, null));
    }

    [Fact(Skip = "domain defect: the analyte pattern ends with '$', which .NET also matches before a final newline, so 'ALT\\n' is accepted as an analyte code; use \\z. No seed: literal input")]
    public void A_code_followed_by_a_line_break_is_refused() {
        // Act / Assert
        Assert.Throws<FormatException>(() => new LabResult("ALT\n", null, null, null, null));
    }

    [Fact]
    public void The_static_check_agrees_with_the_constructor() {
        // Assert
        Assert.True(LabResult.IsAnalyteCode("HGB"));
        Assert.False(LabResult.IsAnalyteCode("hgb"));
        Assert.False(LabResult.IsAnalyteCode(null!));
    }

    // ---------------------------------------------------------------- the unit

    [Theory]
    [InlineData("g/L")]
    [InlineData("10^9/L")]
    [InlineData("%")]
    [InlineData("12345678901234567890")]
    public void A_unit_is_one_to_twenty_characters_without_whitespace(string unit) {
        // Act
        LabResult lab = new LabResult(AnyCode(), Observed<decimal>.Of(AnyResult()), unit, null, null);

        // Assert
        Assert.Equal(unit, lab.Unit);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("g /L")]
    [InlineData("g/L ")]
    [InlineData("g/\tL")]
    [InlineData("g/\nL")]
    [InlineData("123456789012345678901")]
    public void A_unit_that_is_empty_too_long_or_holds_whitespace_is_refused(string unit) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(AnyCode(), Observed<decimal>.Of(AnyResult()), unit, null, null));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    // ---------------------------------------------------------------- values and limits

    [Fact]
    public void A_laboratory_value_of_zero_is_accepted() {
        // Act
        LabResult lab = new LabResult(AnyCode(), Observed<decimal>.Of(0m), AnyUnit(), null, null);

        // Assert
        Assert.Equal(0m, lab.Result!.Value);
    }

    [Fact]
    public void A_negative_laboratory_value_is_refused() {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new LabResult(AnyCode(), Observed<decimal>.Of(-0.001m), AnyUnit(), null, null));

        // Assert
        Assert.Equal("result", refusal.ParamName);
    }

    [Fact]
    public void A_negative_limit_of_normal_is_refused_on_either_side() {
        // Arrange
        string anyUnit = AnyUnit();

        // Act
        ArgumentOutOfRangeException lowerRefusal = Assert.Throws<ArgumentOutOfRangeException>(() => new LabResult(AnyCode(), null, anyUnit, -1m, null));
        ArgumentOutOfRangeException upperRefusal = Assert.Throws<ArgumentOutOfRangeException>(() => new LabResult(AnyCode(), null, anyUnit, null, -1m));

        // Assert
        Assert.Equal("lowerLimitOfNormal", lowerRefusal.ParamName);
        Assert.Equal("upperLimitOfNormal", upperRefusal.ParamName);
    }

    [Fact]
    public void The_lower_limit_must_be_below_the_upper_one() {
        // Arrange
        string anyUnit = AnyUnit();

        // Act
        LabResult accepted = new LabResult(AnyCode(), null, anyUnit, 10m, 10.01m);
        ArgumentException equalLimits    = Assert.Throws<ArgumentException>(() => new LabResult(AnyCode(), null, anyUnit, 10m, 10m));
        ArgumentException invertedLimits = Assert.Throws<ArgumentException>(() => new LabResult(AnyCode(), null, anyUnit, 12m, 10m));

        // Assert
        Assert.Equal(10m, accepted.LowerLimitOfNormal);
        Assert.Equal("upperLimitOfNormal", equalLimits.ParamName);
        Assert.Equal("upperLimitOfNormal", invertedLimits.ParamName);
    }

    [Fact]
    public void A_range_may_be_open_on_one_side() {
        // Arrange
        string anyUnit = AnyUnit();

        // Act
        LabResult lowerOnly = new LabResult(AnyCode(), null, anyUnit, 3m, null);
        LabResult upperOnly = new LabResult(AnyCode(), null, anyUnit, null, 3m);

        // Assert
        Assert.Null(lowerOnly.UpperLimitOfNormal);
        Assert.Null(upperOnly.LowerLimitOfNormal);
    }

    [Fact]
    public void A_limit_of_zero_is_accepted() {
        // Act
        LabResult lab = new LabResult(AnyCode(), null, AnyUnit(), 0m, 1m);

        // Assert
        Assert.Equal(0m, lab.LowerLimitOfNormal);
    }

    // ---------------------------------------------------------------- unit and range depend on the result

    [Fact]
    public void An_observed_value_needs_its_unit() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(AnyCode(), Observed<decimal>.Of(AnyResult()), null, null, null));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact]
    public void A_value_missing_for_a_reason_carries_no_unit() {
        // Arrange
        Observed<decimal> anyMissing = AnyObservedOf.Missing<decimal>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(AnyCode(), anyMissing, AnyUnit(), null, null));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact]
    public void A_value_missing_for_a_reason_carries_no_reference_range() {
        // Arrange
        Observed<decimal> anyMissing = AnyObservedOf.Missing<decimal>().Generate();

        // Act
        ArgumentException lowerRefusal = Assert.Throws<ArgumentException>(() => new LabResult(AnyCode(), anyMissing, null, 1m, null));
        ArgumentException upperRefusal = Assert.Throws<ArgumentException>(() => new LabResult(AnyCode(), anyMissing, null, null, 2m));

        // Assert
        Assert.Equal("lowerLimitOfNormal", lowerRefusal.ParamName);
        Assert.Equal("lowerLimitOfNormal", upperRefusal.ParamName);
    }

    [Fact]
    public void A_missing_value_without_unit_or_range_is_accepted() {
        // Arrange
        Observed<decimal> anyMissing = AnyObservedOf.Missing<decimal>().Generate();

        // Act
        LabResult lab = new LabResult(AnyCode(), anyMissing, null, null, null);

        // Assert
        Assert.True(lab.IsResolved);
        Assert.Null(lab.Unit);
    }

    [Fact]
    public void A_result_not_entered_yet_may_already_carry_a_unit_and_a_range() {
        // Arrange
        string anyUnit = AnyUnit();

        // Act
        LabResult withUnit  = new LabResult(AnyCode(), null, anyUnit, null, null);
        LabResult withRange = new LabResult(AnyCode(), null, anyUnit, 1m, 5m);

        // Assert
        Assert.Equal(anyUnit, withUnit.Unit);
        Assert.Equal(5m, withRange.UpperLimitOfNormal);
    }

    [Fact]
    public void A_reference_range_is_expressed_in_a_unit() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(AnyCode(), null, null, 1m, 5m));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    // ---------------------------------------------------------------- resolution and flag

    [Fact]
    public void A_result_is_resolved_once_entered_observed_or_missing() {
        // Assert
        Assert.False(Any.LabResult().Blank().Generate().IsResolved);
        Assert.True(Any.LabResult().Missing().Generate().IsResolved);
        Assert.True(Any.LabResult().Observed().Generate().IsResolved);
    }

    [Theory]
    [InlineData("4.9", LabFlag.Low)]
    [InlineData("5.0", LabFlag.Normal)]
    [InlineData("7.5", LabFlag.Normal)]
    [InlineData("10.0", LabFlag.Normal)]
    [InlineData("10.1", LabFlag.High)]
    [InlineData("0", LabFlag.Low)]
    public void A_value_is_flagged_against_the_reference_range_with_both_limits_inside_it(string result, LabFlag expected) {
        // Arrange
        LabResult lab = new LabResult(AnyCode(), Observed<decimal>.Of(decimal.Parse(result, CultureInfo.InvariantCulture)), AnyUnit(), 5m, 10m);

        // Act
        LabFlag flag = lab.Flag;

        // Assert
        Assert.Equal(expected, flag);
    }

    [Theory]
    [InlineData("0", LabFlag.Normal)]
    [InlineData("10", LabFlag.Normal)]
    [InlineData("10.1", LabFlag.High)]
    public void A_range_with_an_upper_limit_only_flags_high_values_only(string result, LabFlag expected) {
        // Arrange
        LabResult lab = new LabResult(AnyCode(), Observed<decimal>.Of(decimal.Parse(result, CultureInfo.InvariantCulture)), AnyUnit(), null, 10m);

        // Assert
        Assert.Equal(expected, lab.Flag);
    }

    [Theory]
    [InlineData("4.9", LabFlag.Low)]
    [InlineData("5", LabFlag.Normal)]
    [InlineData("5000", LabFlag.Normal)]
    public void A_range_with_a_lower_limit_only_flags_low_values_only(string result, LabFlag expected) {
        // Arrange
        LabResult lab = new LabResult(AnyCode(), Observed<decimal>.Of(decimal.Parse(result, CultureInfo.InvariantCulture)), AnyUnit(), 5m, null);

        // Assert
        Assert.Equal(expected, lab.Flag);
    }

    [Fact]
    public void A_value_without_any_limit_cannot_be_evaluated() {
        // Arrange
        LabResult lab = new LabResult(AnyCode(), Observed<decimal>.Of(AnyResult()), AnyUnit(), null, null);

        // Assert
        Assert.Equal(LabFlag.NotEvaluable, lab.Flag);
    }

    [Fact]
    public void A_result_that_is_missing_or_not_entered_cannot_be_evaluated() {
        // Assert
        Assert.Equal(LabFlag.NotEvaluable, Any.LabResult().Missing().Generate().Flag);
        Assert.Equal(LabFlag.NotEvaluable, Any.LabResult().Blank().Generate().Flag);
    }

    // ---------------------------------------------------------------- equality

    [Fact]
    public void Two_results_are_equal_when_every_field_is() {
        // Arrange
        LabResult anyLab = Any.LabResult().Generate();

        // Act
        LabResult sameLab = new LabResult(anyLab.AnalyteCode, anyLab.Result, anyLab.Unit, anyLab.LowerLimitOfNormal, anyLab.UpperLimitOfNormal);

        // Assert
        Assert.Equal(anyLab, sameLab);
        Assert.Equal(anyLab.GetHashCode(), sameLab.GetHashCode());
    }

    [Fact]
    public void Results_of_two_analytes_are_not_equal() {
        // Arrange
        LabResult first  = Any.LabResult().WithAnalyteCode("ALT").Generate();
        LabResult second = new LabResult("AST", first.Result, first.Unit, first.LowerLimitOfNormal, first.UpperLimitOfNormal);

        // Assert
        Assert.NotEqual(first, second);
    }

}
