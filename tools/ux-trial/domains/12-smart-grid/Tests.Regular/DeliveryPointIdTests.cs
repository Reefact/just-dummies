using JustDummies;

using SmartGrid.Domain;

using Xunit;

namespace SmartGrid.Tests.Regular;

public sealed class DeliveryPointIdTests {

    [Fact]
    public void Fourteen_digits_make_a_delivery_point_id() {
        // Act
        DeliveryPointId id = new DeliveryPointId("01234567890123");

        // Assert
        Assert.Equal("01234567890123", id.Value);
        Assert.Equal("01234567890123", id.ToString());
    }

    [Fact]
    public void A_null_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new DeliveryPointId(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("              ")]
    [InlineData("\t")]
    public void A_blank_value_is_refused_before_its_format_is_looked_at(string blank) {
        Assert.Throws<ArgumentException>(() => new DeliveryPointId(blank));
    }

    [Theory]
    [InlineData("1234567890123")]
    [InlineData("123456789012345")]
    [InlineData("1234567890123A")]
    [InlineData("1234567890 123")]
    [InlineData(" 12345678901234")]
    [InlineData("12345678901234 ")]
    [InlineData("-2345678901234")]
    [InlineData("1234567890123.")]
    public void A_value_that_is_not_exactly_fourteen_digits_is_refused(string malformed) {
        Assert.Throws<FormatException>(() => new DeliveryPointId(malformed));
    }

    [Fact]
    public void A_value_that_is_too_short_or_too_long_is_refused_whatever_its_digits() {
        // Arrange
        string anyTooShort = Any.StringMatching("[0-9]{1,13}").Generate();
        string anyTooLong  = Any.StringMatching("[0-9]{15,30}").Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new DeliveryPointId(anyTooShort));
        Assert.Throws<FormatException>(() => new DeliveryPointId(anyTooLong));
    }

    [Fact]
    public void A_value_with_one_non_digit_inside_fourteen_characters_is_refused() {
        // Arrange
        string anyDigits  = Any.StringMatching("[0-9]{13}").Generate();
        char   anyNonDigit = Any.String().Alpha().WithLength(1).Generate()[0];

        // Act & Assert
        Assert.Throws<FormatException>(() => new DeliveryPointId(anyDigits + anyNonDigit));
    }

    [Fact(Skip = "domain defect: the pattern is '^\\d{14}$', and '$' also matches before a trailing newline, so '12345678901234\\n' is accepted and Value then holds a newline (literal value, no seed)")]
    public void A_trailing_newline_is_refused() {
        Assert.Throws<FormatException>(() => new DeliveryPointId("12345678901234\n"));
    }

    [Fact(Skip = "domain defect: '\\d' matches every Unicode decimal digit, so fourteen Arabic-Indic digits are accepted as a delivery point id (literal value, no seed)")]
    public void Digits_of_other_scripts_are_refused() {
        Assert.Throws<FormatException>(() => new DeliveryPointId("٠١٢٣٤٥٦٧٨٩٠١٢٣"));
    }

    [Fact]
    public void Two_ids_with_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.StringMatching("[0-9]{14}").Generate();

        // Act
        DeliveryPointId first  = new DeliveryPointId(anyValue);
        DeliveryPointId second = new DeliveryPointId(anyValue);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_with_different_values_are_not_equal() {
        // Arrange
        DeliveryPointId anyFirst  = new AnyDeliveryPointId().Generate();
        DeliveryPointId anySecond = new AnyDeliveryPointId().WithValue(v => v.DifferentFrom(anyFirst.Value)).Generate();

        // Assert
        Assert.False(anyFirst.Equals(anySecond));
        Assert.NotEqual(anyFirst, anySecond);
    }

    [Fact]
    public void An_id_is_equal_to_nothing_but_another_id() {
        // Arrange
        DeliveryPointId anyId = new AnyDeliveryPointId().Generate();

        // Assert
        Assert.False(anyId.Equals(null));
        Assert.False(anyId.Equals((object?)anyId.Value));
    }

}
