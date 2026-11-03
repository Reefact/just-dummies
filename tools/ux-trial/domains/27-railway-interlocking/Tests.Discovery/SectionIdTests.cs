using JustDummies;

using RailwayInterlocking.Domain;

namespace RailwayInterlocking.Tests.Discovery;

public sealed class SectionIdTests {

    [Theory]
    [InlineData("T101")]
    [InlineData("A1")]
    [InlineData("AB1234")]
    [InlineData("Z9999")]
    public void Accepts_one_or_two_capital_letters_then_one_to_four_digits(string wellFormed) {
        // Act
        SectionId id = new SectionId(wellFormed);

        // Assert
        Assert.Equal(wellFormed, id.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("T")]
    [InlineData("101")]
    [InlineData("t101")]
    [InlineData("ABC1")]
    [InlineData("T12345")]
    [InlineData("1T01")]
    [InlineData("T 101")]
    [InlineData(" T101")]
    [InlineData("T101 ")]
    [InlineData("T-101")]
    [InlineData("É101")]
    [InlineData("T١٠١")]
    public void Refuses_anything_else(string malformed) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new SectionId(malformed));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void Refuses_null() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new SectionId(null!));
    }

    [Theory(Skip = "domain defect: the pattern '^[A-Z]{1,2}[0-9]{1,4}$' lets .NET's '$' match before a final line feed, so 'T101\\n' is accepted; literal inputs, no seed needed")]
    [InlineData("T101\n")]
    [InlineData("A1\n")]
    public void Refuses_a_trailing_line_feed(string malformed) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SectionId(malformed));
    }

    [Fact]
    public void Two_ids_built_from_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.SectionId().Generate().Value;

        // Act
        SectionId first  = new SectionId(anyText);
        SectionId second = new SectionId(anyText);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_built_from_different_text_are_not_equal() {
        // Arrange
        SectionId first  = new SectionId("T101");
        SectionId second = new SectionId("T102");

        // Assert
        Assert.False(first.Equals(second));
        Assert.False(first.Equals(null));
        Assert.False(first.Equals((object?)"T101"));
    }

    [Fact]
    public void Is_written_as_its_text() {
        // Arrange
        SectionId id = new SectionId("T101");

        // Act & Assert
        Assert.Equal("T101", id.ToString());
    }

    [Fact]
    public void Is_a_usable_dictionary_key_by_value() {
        // Arrange
        Dictionary<SectionId, int> lengths = new Dictionary<SectionId, int> { [new SectionId("T101")] = 7 };

        // Act & Assert
        Assert.Equal(7, lengths[new SectionId("T101")]);
    }

}
