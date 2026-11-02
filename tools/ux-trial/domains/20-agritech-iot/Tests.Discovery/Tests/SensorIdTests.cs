using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests;

public sealed class SensorIdTests {

    [Theory]
    [InlineData("AG-000000")]
    [InlineData("AG-123456")]
    [InlineData("AG-ABCDEF")]
    [InlineData("AG-09AF9A")]
    public void An_id_is_AG_followed_by_six_digits_or_upper_case_A_to_F(string text) {
        SensorId id = new SensorId(text);

        Assert.Equal(text, id.Value);
    }

    [Fact]
    public void A_null_id_is_refused() {
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new SensorId(null!));

        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_id_is_refused(string text) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new SensorId(text));

        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("AG-12345")]      // one digit short
    [InlineData("AG-1234567")]    // one digit too many
    [InlineData("XX-123456")]     // wrong prefix
    [InlineData("ag-123456")]     // the prefix is ordinal: case matters
    [InlineData("AG_123456")]     // wrong separator
    [InlineData("123456789")]     // no prefix at all
    [InlineData("AG-12345G")]     // G is not hexadecimal
    [InlineData("AG-12345a")]     // lower-case hex is refused
    [InlineData("AG-12 456")]     // a space inside
    [InlineData("AG-12345٣")]     // an Arabic-Indic digit is not an ASCII digit
    [InlineData(" AG-12345")]     // leading space shifts everything
    public void A_malformed_id_is_refused(string text) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new SensorId(text));

        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void An_id_too_short_is_refused() {
        // Arrange
        string anyShortSuffix = Any.StringMatching("[0-9A-F]{0,5}").Generate();

        // Act
        Action construction = () => new SensorId("AG-" + anyShortSuffix);

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void An_id_with_a_non_hexadecimal_character_is_refused() {
        // Arrange
        string anyDigits  = Any.StringMatching("[0-9A-F]{5}").Generate();
        string anyIntruder = Any.StringMatching("[G-Z]").Generate();

        // Act
        Action construction = () => new SensorId("AG-" + anyDigits + anyIntruder);

        // Assert
        Assert.Throws<ArgumentException>(construction);
    }

    [Fact]
    public void Two_ids_with_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.SensorId().Generate().Value;

        // Act
        SensorId first  = new SensorId(anyText);
        SensorId second = new SensorId(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_with_different_texts_are_not_equal() {
        // Arrange
        SensorId anyFirst  = Any.SensorId().Generate();
        SensorId anySecond = new SensorId(Any.StringMatching("AG-[0-9A-F]{6}").DifferentFrom(anyFirst.Value).Generate());

        // Assert
        Assert.NotEqual(anyFirst, anySecond);
        Assert.False(anyFirst.Equals(null));
    }

    [Fact]
    public void An_id_prints_as_its_text() {
        Assert.Equal("AG-0A1B2C", new SensorId("AG-0A1B2C").ToString());
    }

}
