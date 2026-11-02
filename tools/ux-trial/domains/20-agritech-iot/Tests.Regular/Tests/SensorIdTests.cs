using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class SensorIdTests {

    [Fact]
    public void A_well_formed_identifier_is_kept_as_given() {
        // Arrange
        string anyValue = Any.StringMatching("AG-[0-9A-F]{6}").Generate();

        // Act
        SensorId anySensorId = new SensorId(anyValue);

        // Assert
        Assert.Equal(anyValue, anySensorId.Value);
        Assert.Equal(anyValue, anySensorId.ToString());
    }

    [Theory]
    [InlineData("AG-000000")]
    [InlineData("AG-999999")]
    [InlineData("AG-AAAAAA")]
    [InlineData("AG-FFFFFF")]
    public void The_extreme_digits_of_each_family_are_accepted(string value) {
        // Act
        SensorId sensorId = new SensorId(value);

        // Assert
        Assert.Equal(value, sensorId.Value);
    }

    [Fact]
    public void A_null_identifier_is_refused() {
        // Act
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SensorId(null!));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("         ")]
    [InlineData("\t\n")]
    public void A_blank_identifier_is_refused(string blank) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new SensorId(blank));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void An_identifier_that_does_not_start_with_the_AG_prefix_is_refused() {
        // Arrange
        string anyWrongPrefix = Any.StringMatching("[B-Z][A-Z]-[0-9A-F]{6}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SensorId(anyWrongPrefix));
    }

    [Theory]
    [InlineData("ag-123456")]
    [InlineData("Ag-123456")]
    [InlineData("AG_123456")]
    [InlineData("AG 123456")]
    [InlineData("XAG-12345")]
    public void The_prefix_is_matched_exactly_and_case_sensitively(string value) {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SensorId(value));
    }

    [Fact]
    public void An_identifier_shorter_than_nine_characters_is_refused() {
        // Arrange
        string anyTooShort = Any.StringMatching("AG-[0-9A-F]{0,5}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SensorId(anyTooShort));
    }

    [Fact]
    public void An_identifier_longer_than_nine_characters_is_refused() {
        // Arrange
        string anyTooLong = Any.StringMatching("AG-[0-9A-F]{7,12}").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SensorId(anyTooLong));
    }

    [Fact]
    public void A_character_outside_the_hexadecimal_alphabet_in_the_last_six_is_refused() {
        // Arrange
        string anyNonHexadecimal = Any.StringMatching("AG-([G-Z][0-9A-F]{5}|[0-9A-F][G-Z][0-9A-F]{4}|[0-9A-F]{2}[G-Z][0-9A-F]{3}|[0-9A-F]{3}[G-Z][0-9A-F]{2}|[0-9A-F]{4}[G-Z][0-9A-F]|[0-9A-F]{5}[G-Z])").Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SensorId(anyNonHexadecimal));
    }

    [Theory]
    [InlineData("AG-abcdef")]
    [InlineData("AG-12345a")]
    [InlineData("AG-12345-")]
    [InlineData("AG-12 456")]
    public void Lower_case_digits_and_punctuation_are_refused(string value) {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SensorId(value));
    }

    [Fact]
    public void A_non_ASCII_digit_is_refused() {
        // Arrange: Arabic-Indic digits are digits for char.IsDigit, but not ASCII ones.
        string arabicIndicDigits = "AG-١٢٣٤٥٦";

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SensorId(arabicIndicDigits));
    }

    [Fact]
    public void Two_identifiers_with_the_same_value_are_equal_and_hash_alike() {
        // Arrange
        string   anyValue = Any.StringMatching("AG-[0-9A-F]{6}").Generate();
        SensorId anyFirst = new SensorId(anyValue);
        SensorId anySame  = new SensorId(anyValue);

        // Act / Assert
        Assert.True(anyFirst.Equals(anySame));
        Assert.True(anyFirst.Equals((object)anySame));
        Assert.Equal(anyFirst.GetHashCode(), anySame.GetHashCode());
    }

    [Fact]
    public void Two_identifiers_with_different_values_are_not_equal() {
        // Arrange
        string   anyValue     = Any.StringMatching("AG-[0-9A-F]{6}").Generate();
        string   anotherValue = Any.StringMatching("AG-[0-9A-F]{6}").DifferentFrom(anyValue).Generate();
        SensorId anyFirst     = new SensorId(anyValue);
        SensorId anyOther     = new SensorId(anotherValue);

        // Act / Assert
        Assert.False(anyFirst.Equals(anyOther));
        Assert.False(anyFirst.Equals((object)anyOther));
    }

    [Fact]
    public void An_identifier_is_not_equal_to_null_nor_to_another_type() {
        // Arrange
        SensorId anySensorId = Any.SensorId().Generate();

        // Act / Assert
        Assert.False(anySensorId.Equals((SensorId?)null));
        Assert.False(anySensorId.Equals((object?)null));
        Assert.False(anySensorId.Equals(anySensorId.Value));
    }

}
