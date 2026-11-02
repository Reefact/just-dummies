using JustDummies;

using Xunit;

namespace Booking.Domain.Tests;

public sealed class GuestNameTests {

    [Fact]
    public void An_absent_name_is_refused_as_null() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new GuestName(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\r\n")]
    public void A_blank_name_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new GuestName(blank));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_name_of_sixty_characters_is_accepted() {
        // Arrange
        string anySixtyLetters = Any.String().Alpha().WithLength(GuestName.MaxLength).Generate();

        // Act
        GuestName name = new GuestName(anySixtyLetters);

        // Assert
        Assert.Equal(GuestName.MaxLength, name.Value.Length);
    }

    [Fact]
    public void A_name_of_sixty_one_characters_is_refused() {
        // Arrange
        string anySixtyOneLetters = Any.String().Alpha().WithLength(GuestName.MaxLength + 1).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new GuestName(anySixtyOneLetters));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void The_length_limit_applies_to_the_raw_text_before_trimming() {
        // Arrange: sixty letters fit once trimmed, but the raw text is sixty-one characters long
        string anySixtyLetters = Any.String().Alpha().WithLength(GuestName.MaxLength).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new GuestName(anySixtyLetters + " "));
    }

    [Fact]
    public void Padding_inside_the_limit_is_accepted_and_trimmed() {
        // Arrange: raw length is exactly sixty, ten of which are padding
        string anyFiftyLetters = Any.String().Alpha().WithLength(GuestName.MaxLength - 10).Generate();

        // Act
        GuestName name = new GuestName("     " + anyFiftyLetters + "     ");

        // Assert
        Assert.Equal(GuestName.MaxLength - 10, name.Value.Length);
    }

    [Fact]
    public void Surrounding_whitespace_is_removed() {
        // Act
        GuestName name = new GuestName("  \tMarie-Claire \n");

        // Assert
        Assert.Equal("Marie-Claire", name.Value);
    }

    [Fact]
    public void Interior_whitespace_is_kept() {
        // Act
        GuestName name = new GuestName("  de  la Cruz ");

        // Assert
        Assert.Equal("de  la Cruz", name.Value);
    }

    [Fact]
    public void A_name_reads_as_its_trimmed_value() {
        // Act
        GuestName name = new GuestName(" Ada ");

        // Assert
        Assert.Equal("Ada", name.ToString());
    }

}
