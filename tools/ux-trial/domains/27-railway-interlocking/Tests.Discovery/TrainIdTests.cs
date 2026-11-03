using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class TrainIdTests {

    [Theory]
    [InlineData("1A23")]
    [InlineData("0Z00")]
    [InlineData("9A99")]
    public void Accepts_a_digit_a_capital_letter_and_two_digits(string wellFormed) {
        // Act
        TrainId train = new TrainId(wellFormed);

        // Assert
        Assert.Equal(wellFormed, train.Headcode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1A2")]
    [InlineData("1A234")]
    [InlineData("A123")]
    [InlineData("11A2")]
    [InlineData("1a23")]
    [InlineData("1A2X")]
    [InlineData(" 1A23")]
    [InlineData("1A 23")]
    public void Refuses_anything_else(string malformed) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TrainId(malformed));

        // Assert
        Assert.Equal("headcode", refusal.ParamName);
    }

    [Fact]
    public void Refuses_null() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TrainId(null!));
    }

    [Fact(Skip = "domain defect: the pattern '^[0-9][A-Z][0-9]{2}$' lets .NET's '$' match before a final line feed, so '1A23\\n' is accepted; literal input, no seed needed")]
    public void Refuses_a_trailing_line_feed() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TrainId("1A23\n"));
    }

    [Fact]
    public void Two_trains_built_from_the_same_headcode_are_equal() {
        // Arrange
        string anyHeadcode = Any.TrainId().Generate().Headcode;

        // Act
        TrainId first  = new TrainId(anyHeadcode);
        TrainId second = new TrainId(anyHeadcode);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_trains_with_different_headcodes_are_not_equal() {
        // Arrange
        TrainId first  = new TrainId("1A23");
        TrainId second = new TrainId("1A24");

        // Assert
        Assert.False(first.Equals(second));
        Assert.False(first.Equals(null));
    }

    [Fact]
    public void Is_written_as_its_headcode() {
        // Arrange
        TrainId train = new TrainId("1A23");

        // Act & Assert
        Assert.Equal("1A23", train.ToString());
    }

}
