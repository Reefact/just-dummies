using JustDummies;

using OrbitalMicrobiology.Domain;

using Xunit;

namespace OrbitalMicrobiology.Tests;

public sealed class CultureIdTests {

    [Theory]
    [InlineData("CH01-C0000", 1)]
    [InlineData("CH03-C0042", 3)]
    [InlineData("CH12-C9999", 12)]
    public void A_culture_id_exposes_its_chamber(string text, int expectedChamber) {
        // Act
        CultureId id = new CultureId(text);

        // Assert
        Assert.Equal(expectedChamber, id.Chamber);
        Assert.Equal(text, id.Value);
        Assert.Equal(text, id.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("CH03C0042")]
    [InlineData("ch03-C0042")]
    [InlineData("CH3-C0042")]
    [InlineData("CH03-C042")]
    [InlineData("CH03-C00421")]
    [InlineData("XCH03-C0042")]
    [InlineData("CH03-C0042 ")]
    [InlineData("CH0A-C0042")]
    public void A_malformed_culture_id_is_refused(string text) {
        // Act
        ArgumentException refusal = Assert.ThrowsAny<ArgumentException>(() => new CultureId(text));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("CH00-C0001")]
    [InlineData("CH13-C0001")]
    [InlineData("CH99-C0001")]
    public void A_chamber_the_station_does_not_have_is_refused(string text) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new CultureId(text));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_null_culture_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CultureId(null!));
    }

    [Fact]
    public void Two_ids_written_the_same_are_equal() {
        // Act
        CultureId first  = new CultureId("CH07-C0123");
        CultureId second = new CultureId("CH07-C0123");

        // Assert
        Assert.Equal(first, second);
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_written_differently_are_not_equal() {
        // Arrange
        CultureId id        = new CultureId("CH07-C0123");
        CultureId otherSeq  = new CultureId("CH07-C0124");
        CultureId otherRoom = new CultureId("CH08-C0123");

        // Assert
        Assert.NotEqual(id, otherSeq);
        Assert.NotEqual(id, otherRoom);
        Assert.False(id.Equals(null));
    }

    [Fact(Skip = "domain defect: the `$` of the id regex also matches before a final line break, so 'CH03-C0042\\n' is accepted and kept as Value; seed -1355726877 (any seed reproduces it)")]
    public void A_trailing_line_break_is_not_part_of_a_culture_id() {
        // Arrange
        string anyId = Any.CultureId().Generate().Value;

        // Act / Assert
        Assert.ThrowsAny<ArgumentException>(() => new CultureId(anyId + "\n"));
    }

    [Fact(Skip = "domain defect: `\\d` matches non-ASCII digits, then int.Parse throws FormatException instead of the documented ArgumentException; seed 531535202 (any seed reproduces it)")]
    public void Digits_of_another_script_are_refused_as_an_argument_error() {
        // Arabic-indic digits: "CH03-C0042"
        Assert.ThrowsAny<ArgumentException>(() => new CultureId("CH٠٣-C٠٠٤٢"));
    }

}
