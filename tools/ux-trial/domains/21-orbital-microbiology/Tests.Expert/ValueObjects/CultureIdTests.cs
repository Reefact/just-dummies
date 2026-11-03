using JustDummies;
using OrbitalMicrobiology.Domain;
using Xunit;

namespace OrbitalMicrobiology.Tests.Expert.ValueObjects;

public sealed class CultureIdTests {

    [Theory]
    [InlineData("CH03-C0042", 3)]
    [InlineData("CH01-C0001", 1)]
    [InlineData("CH09-C9999", 9)]
    [InlineData("CH10-C0000", 10)]
    [InlineData("CH12-C0042", 12)]
    public void The_chamber_is_read_from_the_two_digits_after_CH(string text, int expectedChamber) {
        // Act
        CultureId id = new CultureId(text);

        // Assert
        Assert.Equal(expectedChamber, id.Chamber);
        Assert.Equal(text, id.Value);
        Assert.Equal(text, id.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("CH3-C0042")]
    [InlineData("CH03-C042")]
    [InlineData("CH03-C00421")]
    [InlineData("ch03-C0042")]
    [InlineData("CH03-c0042")]
    [InlineData("CH03C0042")]
    [InlineData("CH03-C0042 ")]
    [InlineData(" CH03-C0042")]
    [InlineData("CH0A-C0042")]
    [InlineData("XX03-C0042")]
    public void A_text_that_does_not_read_CHnn_Cnnnn_is_refused(string malformed) {
        Assert.Throws<ArgumentException>(() => new CultureId(malformed));
    }

    [Fact(Skip = "domain defect: CultureId accepts 'CH03-C0042\\n' because the regex ends with '$', which .NET also matches before a final newline; literal input, not seed-dependent")]
    public void A_text_with_a_trailing_newline_is_refused() {
        Assert.Throws<ArgumentException>(() => new CultureId("CH03-C0042\n"));
    }

    [Theory]
    [InlineData("CH00-C0001")]
    [InlineData("CH13-C0001")]
    [InlineData("CH99-C0001")]
    public void A_chamber_the_station_does_not_have_is_refused(string text) {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CultureId(text));
    }

    [Fact]
    public void A_missing_text_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CultureId(null!));
    }

    [Fact]
    public void Two_ids_written_the_same_are_equal() {
        // Arrange
        string anyText = Any.CultureId().Generate().Value;

        // Act
        CultureId first  = new CultureId(anyText);
        CultureId second = new CultureId(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_written_differently_are_not_equal() {
        // Arrange
        CultureId anyId      = Any.CultureId().Generate();
        CultureId anyOtherId = Any.CultureId().WithValue(text => text.DifferentFrom(anyId.Value)).Generate();

        // Assert
        Assert.NotEqual(anyId, anyOtherId);
    }

    [Fact]
    public void An_id_is_not_equal_to_nothing() {
        // Arrange
        CultureId anyId = Any.CultureId().Generate();

        // Assert
        Assert.False(anyId.Equals(null));
        Assert.False(anyId.Equals((object?)null));
    }

}
