using JustDummies;

using Xunit;

namespace Booking.Domain;

public sealed class GuestNameTests {

    [Fact]
    public void A_name_is_stored_trimmed() {
        // Act
        GuestName name = new GuestName("  Marie Curie \t");

        // Assert
        Assert.Equal("Marie Curie", name.Value);
        Assert.Equal("Marie Curie", name.ToString());
    }

    [Fact]
    public void A_name_of_exactly_the_maximum_length_is_accepted() {
        // Arrange
        string anyMaximumName = Any.String().Alpha().WithLength(GuestName.MaxLength).Generate();

        // Act
        GuestName name = new GuestName(anyMaximumName);

        // Assert
        Assert.Equal(anyMaximumName, name.Value);
    }

    [Fact]
    public void A_name_one_character_too_long_is_refused() {
        // Arrange
        string anyTooLongName = Any.String().Alpha().WithLength(GuestName.MaxLength + 1).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GuestName(anyTooLongName));
    }

    [Fact]
    public void The_length_is_measured_before_trimming() {
        // Arrange: 60 characters once trimmed, 62 raw.
        string anyCoreName = Any.String().Alpha().WithLength(GuestName.MaxLength).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GuestName(" " + anyCoreName + " "));
    }

    [Fact]
    public void A_null_name_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new GuestName(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\r\n")]
    public void A_blank_name_is_refused(string blank) {
        Assert.Throws<ArgumentException>(() => new GuestName(blank));
    }

    [Fact]
    public void A_name_can_hold_inner_whitespace_and_punctuation() {
        // Act
        GuestName name = new GuestName("Jean-Pierre O'Neil");

        // Assert
        Assert.Equal("Jean-Pierre O'Neil", name.Value);
    }

}
