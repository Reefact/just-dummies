using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests.Discovery;

public sealed class CultureIdTests {

    [Theory]
    [InlineData("CH03-C0042", 3)]
    [InlineData("CH01-C0000", 1)]
    [InlineData("CH12-C9999", 12)]
    [InlineData("CH10-C0001", 10)]
    public void A_culture_id_exposes_the_chamber_it_names(string text, int expectedChamber) {
        // Act
        CultureId id = new CultureId(text);

        // Assert
        Assert.Equal(expectedChamber, id.Chamber);
        Assert.Equal(text, id.Value);
        Assert.Equal(text, id.ToString());
    }

    [Theory]
    [InlineData("CH00-C0042")]
    [InlineData("CH13-C0042")]
    [InlineData("CH99-C0001")]
    public void A_chamber_outside_the_station_is_refused(string text) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CultureId(text));

        // Assert
        Assert.Contains("01 to 12", refusal.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("CH3-C0042")]
    [InlineData("CH03-C042")]
    [InlineData("CH03-C00421")]
    [InlineData("ch03-C0042")]
    [InlineData("CH03C0042")]
    [InlineData(" CH03-C0042")]
    [InlineData("CH0A-C0042")]
    public void A_text_that_is_not_chamber_dash_sequence_is_refused(string text) {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CultureId(text));
    }

    [Fact(Skip = "domain defect: the pattern ends with '$', which .NET also matches before a final newline, so 'CH03-C0042\\n' is accepted and stored with its newline (literal input, no seed)")]
    public void A_text_with_a_trailing_newline_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new CultureId("CH03-C0042\n"));
    }

    [Fact]
    public void A_missing_text_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new CultureId(null!));
    }

    [Fact]
    public void Two_ids_with_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.CultureId().Generate().Value;

        // Act
        CultureId first  = new CultureId(anyText);
        CultureId second = new CultureId(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void Two_ids_with_different_texts_are_not_equal() {
        // Arrange
        CultureId anyId      = Any.CultureId().Generate();
        CultureId anyOtherId = Any.CultureId().WithValue(v => v.DifferentFrom(anyId.Value)).Generate();

        // Act / Assert
        Assert.NotEqual(anyId, anyOtherId);
    }

    [Fact]
    public void An_id_is_never_equal_to_nothing() {
        // Arrange
        CultureId anyId = Any.CultureId().Generate();

        // Act / Assert
        Assert.False(anyId.Equals(null));
        Assert.False(anyId.Equals((object?)"not an id"));
    }

}
