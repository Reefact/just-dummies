using AgriTech.Domain.Tests.Expert.Generators;

using JustDummies;

using Xunit;

namespace AgriTech.Domain.Tests.Expert;

public sealed class SensorIdTests {

    [Theory]
    [InlineData("AG-000000")]
    [InlineData("AG-FFFFFF")]
    [InlineData("AG-09AF5C")]
    public void A_sensor_id_is_AG_followed_by_six_digits_or_upper_case_A_to_F(string value) {
        // Act
        SensorId sensorId = new SensorId(value);

        // Assert
        Assert.Equal(value, sensorId.Value);
    }

    [Fact]
    public void A_null_value_is_refused_as_a_missing_argument() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new SensorId(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("         ")]
    [InlineData("\t\n")]
    public void A_blank_value_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new SensorId(blank));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("AG-")]
    [InlineData("AG-12345")]
    [InlineData("AG-1234567")]
    [InlineData("AG-12345678901234")]
    public void A_value_that_is_not_nine_characters_long_is_refused(string wrongLength) {
        Assert.Throws<ArgumentException>(() => new SensorId(wrongLength));
    }

    [Theory]
    [InlineData("ag-123456")]
    [InlineData("Ag-123456")]
    [InlineData("AX-123456")]
    [InlineData("AG_123456")]
    [InlineData("AG:123456")]
    [InlineData(" AG-12345")]
    [InlineData("123456AG-")]
    public void A_value_that_does_not_start_with_the_AG_prefix_in_upper_case_is_refused(string wrongPrefix) {
        Assert.Throws<ArgumentException>(() => new SensorId(wrongPrefix));
    }

    [Theory]
    [InlineData("AG-12345a")]
    [InlineData("AG-abcdef")]
    [InlineData("AG-12345G")]
    [InlineData("AG-12 456")]
    [InlineData("AG-12345-")]
    [InlineData("AG-1234٣5")] // an Arabic-Indic digit three: a digit for char.IsDigit, not an ASCII one
    public void A_suffix_that_is_not_made_of_digits_and_upper_case_A_to_F_is_refused(string wrongSuffix) {
        Assert.Throws<ArgumentException>(() => new SensorId(wrongSuffix));
    }

    [Fact]
    public void Two_ids_built_from_the_same_value_are_equal() {
        // Arrange
        string anyValue = Any.SensorId().Generate().Value;

        // Act
        SensorId first  = new SensorId(anyValue);
        SensorId second = new SensorId(anyValue);

        // Assert
        Assert.NotSame(first, second);
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_built_from_different_values_are_not_equal() {
        // Arrange
        SensorId anyId      = Any.SensorId().Generate();
        SensorId anyOtherId = Any.SensorId().WithValue(value => value.DifferentFrom(anyId.Value)).Generate();

        // Assert
        Assert.NotEqual(anyId, anyOtherId);
        Assert.False(anyId.Equals((object)anyOtherId));
    }

    [Fact]
    public void An_id_is_equal_to_neither_null_nor_to_something_that_is_not_an_id() {
        // Arrange
        SensorId anyId = Any.SensorId().Generate();

        // Assert
        Assert.False(anyId.Equals(null));
        Assert.False(anyId.Equals((object?)null));
        Assert.False(anyId.Equals((object)anyId.Value));
    }

    [Fact]
    public void An_id_is_written_as_its_value() {
        // Arrange
        SensorId sensorId = new SensorId("AG-0A1B2C");

        // Assert
        Assert.Equal("AG-0A1B2C", sensorId.ToString());
    }

}
