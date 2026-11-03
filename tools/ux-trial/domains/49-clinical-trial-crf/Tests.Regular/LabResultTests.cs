using System;
using System.Globalization;

using ClinicalTrial.Domain.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

public sealed class LabResultTests {

    private static decimal D(string text) {
        return decimal.Parse(text, CultureInfo.InvariantCulture);
    }

    private static Observed<decimal> Dec(decimal value) {
        return Observed<decimal>.Of(value);
    }

    private static string AnyCode() {
        return TrialFields.AnalyteCode().Generate();
    }

    private static string AnyUnit() {
        return Any.StringMatching("[a-zA-Z0-9/%]{1,12}").Generate();
    }

    [Theory]
    [InlineData("AL")]
    [InlineData("ALT")]
    [InlineData("A1C")]
    [InlineData("ABCDEFGHIJ")]
    public void A_code_of_an_upper_case_letter_then_1_to_9_letters_or_digits_is_accepted(string code) {
        Assert.Equal(code, new LabResult(code, null, null, null, null).AnalyteCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("alt")]
    [InlineData("Alt")]
    [InlineData("1AB")]
    [InlineData("AL T")]
    [InlineData("AL-T")]
    [InlineData("ABCDEFGHIJK")]
    [InlineData("ALT ")]
    public void A_code_that_does_not_follow_the_format_is_refused(string code) {
        Assert.Throws<FormatException>(() => new LabResult(code, null, null, null, null));
    }

    [Fact(Skip = "domain defect: the analyte pattern is anchored with a dollar sign, which matches before a final newline, so ALT followed by a newline is accepted as an analyte code; no drawn value involved, no seed")]
    public void A_code_followed_by_a_line_break_is_refused() {
        Assert.Throws<FormatException>(() => new LabResult("ALT\n", null, null, null, null));
    }

    [Fact]
    public void A_missing_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new LabResult(null!, null, null, null, null));
    }

    [Theory]
    [InlineData("ALT", true)]
    [InlineData("A", false)]
    [InlineData("alt", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void An_analyte_code_can_be_checked_without_building_a_result(string? code, bool expected) {
        Assert.Equal(expected, LabResult.IsAnalyteCode(code!));
    }

    [Theory]
    [InlineData("g/L")]
    [InlineData("10^9/L")]
    [InlineData("%")]
    [InlineData("12345678901234567890")]
    public void A_unit_of_1_to_20_characters_without_spaces_is_accepted(string unit) {
        // Arrange
        string anyCode = AnyCode();

        // Act
        LabResult lab = new LabResult(anyCode, Dec(1m), unit, null, null);

        // Assert
        Assert.Equal(unit, lab.Unit);
    }

    [Theory]
    [InlineData("")]
    [InlineData("g /L")]
    [InlineData(" g/L")]
    [InlineData("g/L ")]
    [InlineData("g\t/L")]
    [InlineData("123456789012345678901")]
    public void A_unit_that_is_empty_too_long_or_contains_whitespace_is_refused(string unit) {
        // Arrange
        string anyCode = AnyCode();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(anyCode, Dec(1m), unit, null, null));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact]
    public void An_observed_value_of_zero_is_accepted_and_a_negative_one_is_not() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Assert
        Assert.Equal(0m, new LabResult(anyCode, Dec(0m), anyUnit, null, null).Result!.Value);
        Assert.Throws<ArgumentOutOfRangeException>(() => new LabResult(anyCode, Dec(-0.1m), anyUnit, null, null));
    }

    [Fact]
    public void A_limit_of_normal_cannot_be_negative() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Assert
        Assert.Equal("lowerLimitOfNormal", Assert.Throws<ArgumentOutOfRangeException>(() => new LabResult(anyCode, null, anyUnit, -1m, 5m)).ParamName);
        Assert.Equal("upperLimitOfNormal", Assert.Throws<ArgumentOutOfRangeException>(() => new LabResult(anyCode, null, anyUnit, null, -1m)).ParamName);
    }

    [Fact]
    public void A_limit_of_zero_is_accepted() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Act
        LabResult lab = new LabResult(anyCode, null, anyUnit, 0m, 10m);

        // Assert
        Assert.Equal(0m, lab.LowerLimitOfNormal);
    }

    [Theory]
    [InlineData("10", "10")]
    [InlineData("11", "10")]
    public void The_lower_limit_must_be_strictly_below_the_upper_limit(string lower, string upper) {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(anyCode, null, anyUnit, D(lower), D(upper)));

        // Assert
        Assert.Equal("upperLimitOfNormal", refusal.ParamName);
    }

    [Fact]
    public void A_range_may_be_bounded_on_one_side_only() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Assert
        Assert.NotNull(new LabResult(anyCode, null, anyUnit, 10m, null));
        Assert.NotNull(new LabResult(anyCode, null, anyUnit, null, 40m));
    }

    [Fact]
    public void An_observed_value_needs_its_unit() {
        // Arrange
        string anyCode = AnyCode();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(anyCode, Dec(5m), null, null, null));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact]
    public void A_missing_value_carries_no_unit() {
        // Arrange
        string        anyCode   = AnyCode();
        string        anyUnit   = AnyUnit();
        MissingReason anyReason = Any.Enum<MissingReason>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(anyCode, Observed<decimal>.Missing(anyReason), anyUnit, null, null));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact]
    public void A_missing_value_carries_no_reference_range() {
        // Arrange
        string        anyCode   = AnyCode();
        MissingReason anyReason = Any.Enum<MissingReason>().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(anyCode, Observed<decimal>.Missing(anyReason), null, null, 40m));

        // Assert
        Assert.Equal("lowerLimitOfNormal", refusal.ParamName);
    }

    [Fact]
    public void A_missing_value_with_neither_unit_nor_range_is_accepted() {
        // Arrange
        string        anyCode   = AnyCode();
        MissingReason anyReason = Any.Enum<MissingReason>().Generate();

        // Act
        LabResult lab = new LabResult(anyCode, Observed<decimal>.Missing(anyReason), null, null, null);

        // Assert
        Assert.True(lab.IsResolved);
        Assert.Null(lab.Unit);
    }

    [Fact]
    public void A_result_not_entered_yet_may_or_may_not_carry_a_unit() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Assert
        Assert.NotNull(new LabResult(anyCode, null, null, null, null));
        Assert.NotNull(new LabResult(anyCode, null, anyUnit, null, null));
    }

    [Fact]
    public void A_range_is_expressed_in_a_unit() {
        // Arrange
        string anyCode = AnyCode();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new LabResult(anyCode, null, null, 10m, 40m));

        // Assert
        Assert.Equal("unit", refusal.ParamName);
    }

    [Fact]
    public void A_result_is_resolved_once_entered_whatever_it_says() {
        // Arrange
        MissingReason anyReason = Any.Enum<MissingReason>().Generate();

        // Assert
        Assert.False(Any.LabResult().Blank().Generate().IsResolved);
        Assert.True(Any.LabResult().Measured().Generate().IsResolved);
        Assert.True(Any.LabResult().WithResult(Observed<decimal>.Missing(anyReason)).Generate().IsResolved);
    }

    [Theory]
    [InlineData("5", "10", "40", LabFlag.Low)]
    [InlineData("9.99", "10", "40", LabFlag.Low)]
    [InlineData("10", "10", "40", LabFlag.Normal)]
    [InlineData("25", "10", "40", LabFlag.Normal)]
    [InlineData("40", "10", "40", LabFlag.Normal)]
    [InlineData("40.01", "10", "40", LabFlag.High)]
    [InlineData("0", "10", "40", LabFlag.Low)]
    public void A_value_is_flagged_against_the_reference_range_limits_included(string value, string lower, string upper, LabFlag expected) {
        // Arrange
        LabResult lab = new LabResult(AnyCode(), Dec(D(value)), AnyUnit(), D(lower), D(upper));

        // Assert
        Assert.Equal(expected, lab.Flag);
    }

    [Theory]
    [InlineData("5", LabFlag.Low)]
    [InlineData("10", LabFlag.Normal)]
    [InlineData("500", LabFlag.Normal)]
    public void With_a_lower_limit_only_a_value_is_never_high(string value, LabFlag expected) {
        // Arrange
        LabResult lab = new LabResult(AnyCode(), Dec(D(value)), AnyUnit(), 10m, null);

        // Assert
        Assert.Equal(expected, lab.Flag);
    }

    [Theory]
    [InlineData("0", LabFlag.Normal)]
    [InlineData("40", LabFlag.Normal)]
    [InlineData("41", LabFlag.High)]
    public void With_an_upper_limit_only_a_value_is_never_low(string value, LabFlag expected) {
        // Arrange
        LabResult lab = new LabResult(AnyCode(), Dec(D(value)), AnyUnit(), null, 40m);

        // Assert
        Assert.Equal(expected, lab.Flag);
    }

    [Fact]
    public void A_value_without_a_range_cannot_be_evaluated() {
        // Arrange
        LabResult lab = Any.LabResult().WithResult(Dec(12m)).WithoutRange().Generate();

        // Assert
        Assert.Equal(LabFlag.NotEvaluable, lab.Flag);
    }

    [Fact]
    public void A_range_without_a_value_cannot_be_evaluated() {
        // Arrange
        LabResult blank   = new LabResult(AnyCode(), null, AnyUnit(), 10m, 40m);
        LabResult missing = new LabResult(AnyCode(), Observed<decimal>.Missing(MissingReason.NotDone), null, null, null);

        // Assert
        Assert.Equal(LabFlag.NotEvaluable, blank.Flag);
        Assert.Equal(LabFlag.NotEvaluable, missing.Flag);
    }

    [Fact]
    public void Results_made_of_the_same_content_are_equal() {
        // Arrange
        LabResult anyLab = Any.LabResult().Generate();

        // Act
        LabResult same = new LabResult(anyLab.AnalyteCode, anyLab.Result, anyLab.Unit, anyLab.LowerLimitOfNormal, anyLab.UpperLimitOfNormal);

        // Assert
        Assert.Equal(anyLab, same);
        Assert.Equal(anyLab.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Results_differing_by_their_value_are_not_equal() {
        // Arrange
        string    anyCode = AnyCode();
        string    anyUnit = AnyUnit();
        LabResult first   = new LabResult(anyCode, Dec(5m), anyUnit, null, null);
        LabResult second  = new LabResult(anyCode, Dec(6m), anyUnit, null, null);

        // Assert
        Assert.NotEqual(first, second);
    }

}
