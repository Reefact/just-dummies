using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

public sealed class LabResultTests {

    private static string AnyCode() => Any.StringMatching("[A-Z][A-Z0-9]{1,9}").Generate();

    private static string AnyUnit() => Any.String().Printable().WithoutChars(" ").WithLengthBetween(1, LabResult.MaxUnitLength).Generate();

    private static Observed<decimal> Value(decimal value) => Observed<decimal>.Of(value);

    private static Observed<decimal> AnyMissing() => Observed<decimal>.Missing(Any.Enum<MissingReason>().Generate());

    private static LabResult Result(decimal value, decimal? lower, decimal? upper) => new LabResult(AnyCode(), Value(value), AnyUnit(), lower, upper);

    [Theory]
    [InlineData("AL")]
    [InlineData("ALT")]
    [InlineData("HGB")]
    [InlineData("A123456789")]
    public void An_analyte_code_is_an_upper_case_letter_then_1_to_9_letters_or_digits(string code) {
        Assert.Equal(code, new LabResult(code, null, null, null, null).AnalyteCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("A")]
    [InlineData("alt")]
    [InlineData("1AB")]
    [InlineData("A-B")]
    [InlineData("A1234567890")]
    [InlineData("ALT ")]
    public void A_text_that_is_not_an_analyte_code_is_refused(string code) {
        Assert.Throws<FormatException>(() => new LabResult(code, null, null, null, null));
    }

    [Fact]
    public void An_analyte_code_is_not_absent() {
        Assert.Throws<ArgumentNullException>(() => new LabResult(null!, null, null, null, null));
    }

    [Fact]
    public void An_observed_value_needs_its_unit() {
        // Arrange
        string anyCode = AnyCode();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, Value(4.2m), null, null, null));
    }

    [Fact]
    public void An_observed_value_with_its_unit_is_accepted() {
        // Act
        LabResult result = new LabResult(AnyCode(), Value(4.2m), "mmol/L", null, null);

        // Assert
        Assert.Equal("mmol/L", result.Unit);
        Assert.Equal(4.2m, result.Result!.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("g /L")]
    [InlineData(" g/L")]
    [InlineData("g/L\t")]
    [InlineData("123456789012345678901")]
    public void A_unit_is_1_to_20_characters_without_whitespace(string unit) {
        // Arrange
        string anyCode = AnyCode();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, Value(4.2m), unit, null, null));
    }

    [Theory]
    [InlineData("g")]
    [InlineData("10^9/L")]
    [InlineData("12345678901234567890")]
    public void A_unit_of_1_to_20_characters_is_accepted(string unit) {
        Assert.Equal(unit, new LabResult(AnyCode(), Value(4.2m), unit, null, null).Unit);
    }

    [Fact]
    public void A_value_missing_for_a_reason_carries_no_unit() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, AnyMissing(), anyUnit, null, null));
    }

    [Fact]
    public void A_value_missing_for_a_reason_carries_no_reference_range() {
        // Arrange
        string anyCode = AnyCode();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, AnyMissing(), null, 1m, 5m));
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, AnyMissing(), null, 1m, null));
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, AnyMissing(), null, null, 5m));
    }

    [Fact]
    public void A_value_missing_for_a_reason_with_neither_unit_nor_range_is_accepted() {
        // Act
        LabResult result = new LabResult(AnyCode(), AnyMissing(), null, null, null);

        // Assert
        Assert.True(result.IsResolved);
        Assert.Null(result.Unit);
    }

    [Fact]
    public void A_blank_result_may_carry_a_unit_and_a_range() {
        // Act
        LabResult result = new LabResult(AnyCode(), null, AnyUnit(), 1m, 5m);

        // Assert
        Assert.False(result.IsResolved);
        Assert.Equal(1m, result.LowerLimitOfNormal);
        Assert.Equal(5m, result.UpperLimitOfNormal);
    }

    [Fact]
    public void A_blank_result_may_carry_no_unit_at_all() {
        Assert.Null(new LabResult(AnyCode(), null, null, null, null).Unit);
    }

    [Fact]
    public void A_reference_range_is_expressed_in_a_unit() {
        // Arrange
        string anyCode = AnyCode();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, null, null, 1m, 5m));
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, null, null, null, 5m));
    }

    [Fact]
    public void A_laboratory_value_is_not_negative() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new LabResult(anyCode, Value(-0.01m), anyUnit, null, null));
        Assert.Equal(0m, new LabResult(anyCode, Value(0m), anyUnit, null, null).Result!.Value);
    }

    [Fact]
    public void A_limit_of_normal_is_not_negative() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Act + Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new LabResult(anyCode, null, anyUnit, -1m, 5m));
        Assert.Throws<ArgumentOutOfRangeException>(() => new LabResult(anyCode, null, anyUnit, null, -1m));
    }

    [Fact]
    public void The_lower_limit_is_strictly_below_the_upper_limit() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, null, anyUnit, 5m, 5m));
        Assert.Throws<ArgumentException>(() => new LabResult(anyCode, null, anyUnit, 6m, 5m));
        Assert.Equal(4.99m, new LabResult(anyCode, null, anyUnit, 4.99m, 5m).LowerLimitOfNormal);
    }

    [Fact]
    public void A_range_may_be_one_sided() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Act
        LabResult lowerOnly = new LabResult(anyCode, null, anyUnit, 1m, null);
        LabResult upperOnly = new LabResult(anyCode, null, anyUnit, null, 5m);

        // Assert
        Assert.Null(lowerOnly.UpperLimitOfNormal);
        Assert.Null(upperOnly.LowerLimitOfNormal);
    }

    [Theory]
    [InlineData("0.9", LabFlag.Low)]
    [InlineData("1.0", LabFlag.Normal)]
    [InlineData("3.0", LabFlag.Normal)]
    [InlineData("5.0", LabFlag.Normal)]
    [InlineData("5.1", LabFlag.High)]
    public void A_value_is_flagged_against_the_reference_range_ends_included(string value, LabFlag expected) {
        decimal measured = decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(expected, Result(measured, 1m, 5m).Flag);
    }

    [Fact]
    public void With_only_a_lower_limit_a_value_is_low_or_normal() {
        Assert.Equal(LabFlag.Low, Result(0.5m, 1m, null).Flag);
        Assert.Equal(LabFlag.Normal, Result(1000m, 1m, null).Flag);
    }

    [Fact]
    public void With_only_an_upper_limit_a_value_is_normal_or_high() {
        Assert.Equal(LabFlag.Normal, Result(0m, null, 5m).Flag);
        Assert.Equal(LabFlag.High, Result(5.5m, null, 5m).Flag);
    }

    [Fact]
    public void Without_a_range_a_value_cannot_be_evaluated() {
        Assert.Equal(LabFlag.NotEvaluable, Result(4.2m, null, null).Flag);
    }

    [Fact]
    public void Without_an_observed_value_a_result_cannot_be_evaluated() {
        Assert.Equal(LabFlag.NotEvaluable, new LabResult(AnyCode(), null, AnyUnit(), 1m, 5m).Flag);
        Assert.Equal(LabFlag.NotEvaluable, new LabResult(AnyCode(), AnyMissing(), null, null, null).Flag);
    }

    [Fact]
    public void A_result_is_resolved_unless_it_is_blank() {
        Assert.True(Result(4.2m, null, null).IsResolved);
        Assert.False(new LabResult(AnyCode(), null, null, null, null).IsResolved);
    }

    [Fact]
    public void Lab_results_with_the_same_content_are_equal() {
        // Arrange
        LabResult anyResult = Any.LabResult().Generate();

        // Act
        LabResult same = new LabResult(anyResult.AnalyteCode, anyResult.Result, anyResult.Unit, anyResult.LowerLimitOfNormal, anyResult.UpperLimitOfNormal);

        // Assert
        Assert.Equal(anyResult, same);
        Assert.Equal(anyResult.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Lab_results_that_differ_by_their_value_are_not_equal() {
        // Arrange
        string anyCode = AnyCode();
        string anyUnit = AnyUnit();

        // Assert
        Assert.NotEqual(new LabResult(anyCode, Value(4.2m), anyUnit, null, null), new LabResult(anyCode, Value(4.3m), anyUnit, null, null));
    }

}
