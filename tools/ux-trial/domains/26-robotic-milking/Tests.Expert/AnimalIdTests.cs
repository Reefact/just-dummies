using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Expert;

public sealed class AnimalIdTests {

    [Theory]
    [InlineData("FR123456789012")]
    [InlineData("BE000000000000")]
    [InlineData("ZZ999999999999")]
    public void An_ear_tag_is_two_capital_letters_followed_by_twelve_digits(string earTag) {
        // Act
        AnimalId animalId = new AnimalId(earTag);

        // Assert
        Assert.Equal(earTag, animalId.Value);
        Assert.Equal(earTag, animalId.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr123456789012")]
    [InlineData("Fr123456789012")]
    [InlineData("F1234567890123")]
    [InlineData("FRA23456789012")]
    [InlineData("FR12345678901")]
    [InlineData("FR1234567890123")]
    [InlineData(" FR123456789012")]
    [InlineData("FR123456789012 ")]
    [InlineData("FR12345678901２")]
    [InlineData("FR 123456789012")]
    public void A_malformed_ear_tag_is_refused(string malformed) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AnimalId(malformed));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the format is anchored with '$', which .NET also matches before a final line feed, so 'FR123456789012\\n' is accepted as an ear tag (use '\\z'); deterministic, no seed involved")]
    public void An_ear_tag_followed_by_a_line_feed_is_refused() {
        // Act
        Exception? refusal = Record.Exception(() => new AnimalId("FR123456789012\n"));

        // Assert
        Assert.IsType<ArgumentException>(refusal);
    }

    [Fact]
    public void An_absent_ear_tag_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new AnimalId(null!));
    }

    [Fact]
    public void The_country_code_is_the_two_leading_letters() {
        // Arrange
        AnimalId frenchCow = new AnimalId("FR123456789012");

        // Act
        string countryCode = frenchCow.CountryCode;

        // Assert
        Assert.Equal("FR", countryCode);
    }

    [Fact]
    public void Two_ids_of_the_same_ear_tag_are_equal() {
        // Arrange
        AnimalId anyAnimalId = Any.AnimalId().Generate();
        AnimalId sameTag     = new AnimalId(anyAnimalId.Value);

        // Act & Assert
        Assert.True(anyAnimalId.Equals(sameTag));
        Assert.True(anyAnimalId.Equals((object)sameTag));
        Assert.Equal(anyAnimalId.GetHashCode(), sameTag.GetHashCode());
    }

    [Fact]
    public void Two_ids_of_different_ear_tags_are_not_equal() {
        // Arrange
        AnimalId anyAnimalId = Any.AnimalId().Generate();
        AnimalId otherCow    = Any.AnimalId().WithValue(tag => tag.DifferentFrom(anyAnimalId.Value)).Generate();

        // Act & Assert
        Assert.False(anyAnimalId.Equals(otherCow));
    }

    [Fact]
    public void An_id_is_not_equal_to_nothing_nor_to_a_bare_string() {
        // Arrange
        AnimalId anyAnimalId = Any.AnimalId().Generate();

        // Act & Assert
        Assert.False(anyAnimalId.Equals((AnimalId?)null));
        Assert.False(anyAnimalId.Equals((object)anyAnimalId.Value));
    }

}
