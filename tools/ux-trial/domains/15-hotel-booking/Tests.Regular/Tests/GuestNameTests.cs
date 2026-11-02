using JustDummies;

using Xunit;

namespace Booking.Domain.Tests.Regular;

public sealed class GuestNameTests {

    [Fact]
    public void A_guest_name_keeps_its_value() {
        // Act
        GuestName name = new GuestName("Ada");

        // Assert
        Assert.Equal("Ada", name.Value);
        Assert.Equal("Ada", name.ToString());
    }

    [Theory]
    [InlineData("  Ada", "Ada")]
    [InlineData("Ada  ", "Ada")]
    [InlineData("\t Ada Lovelace \n", "Ada Lovelace")]
    [InlineData("Jean  Pierre", "Jean  Pierre")]
    public void A_guest_name_is_stored_trimmed(string raw, string stored) {
        // Act
        GuestName name = new GuestName(raw);

        // Assert
        Assert.Equal(stored, name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n\r")]
    [InlineData(" ")]
    public void A_blank_name_is_refused(string blank) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new GuestName(blank));
    }

    [Fact]
    public void A_null_name_is_refused_with_a_null_exception() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new GuestName(null!));
    }

    [Fact]
    public void A_name_of_sixty_characters_is_accepted() {
        // Arrange
        string sixty = new string('a', GuestName.MaxLength);

        // Act
        GuestName name = new GuestName(sixty);

        // Assert
        Assert.Equal(sixty, name.Value);
    }

    [Fact]
    public void A_name_of_sixty_one_characters_is_refused() {
        // Arrange
        string sixtyOne = new string('a', GuestName.MaxLength + 1);

        // Act
        ArgumentOutOfRangeException exception = Assert.Throws<ArgumentOutOfRangeException>(() => new GuestName(sixtyOne));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Fact]
    public void The_length_is_measured_before_trimming() {
        // Arrange: sixty letters once trimmed, sixty-two raw
        string padded = " " + new string('a', GuestName.MaxLength) + " ";

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GuestName(padded));
    }

    [Fact]
    public void A_name_drawn_by_the_generator_is_never_blank_nor_longer_than_the_maximum() {
        // Act
        for (int i = 0; i < 200; i++) {
            GuestName anyName = Arbitrary.GuestName().Generate();

            Assert.False(string.IsNullOrWhiteSpace(anyName.Value));
            Assert.Equal(anyName.Value.Trim(), anyName.Value);
        }
    }

}
