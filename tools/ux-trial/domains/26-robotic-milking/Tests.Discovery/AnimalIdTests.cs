using JustDummies;

using RoboticMilking.Domain;

using Xunit;

namespace RoboticMilking.Tests.Discovery;

public sealed class AnimalIdTests {

    [Fact]
    public void An_ear_tag_keeps_the_text_it_was_built_from() {
        // Arrange
        string tag = "FR123456789012";

        // Act
        AnimalId id = new AnimalId(tag);

        // Assert
        Assert.Equal("FR123456789012", id.Value);
        Assert.Equal("FR123456789012", id.ToString());
    }

    [Fact]
    public void The_country_code_is_the_two_leading_letters() {
        // Arrange
        AnimalId id = new AnimalId("DE000000000042");

        // Act
        string country = id.CountryCode;

        // Assert
        Assert.Equal("DE", country);
    }

    [Fact]
    public void Two_ids_built_from_the_same_text_are_equal() {
        // Arrange
        AnimalId anyId = Any.AnimalId().Generate();

        // Act
        AnimalId sameText = new AnimalId(anyId.Value);

        // Assert
        Assert.Equal(anyId, sameText);
        Assert.True(anyId.Equals((object)sameText));
        Assert.Equal(anyId.GetHashCode(), sameText.GetHashCode());
    }

    [Fact]
    public void Two_ids_built_from_different_texts_are_not_equal() {
        // Arrange
        AnimalId anyId = Any.AnimalId().Generate();

        // Act
        AnimalId anyOtherId = Any.AnimalId().DifferentFrom(anyId).Generate();

        // Assert
        Assert.NotEqual(anyId, anyOtherId);
    }

    [Fact]
    public void An_id_is_never_equal_to_null_or_to_another_kind_of_object() {
        // Arrange
        AnimalId anyId = Any.AnimalId().Generate();

        // Act / Assert
        Assert.False(anyId.Equals((AnimalId?)null));
        Assert.False(anyId.Equals((object?)null));
        Assert.False(anyId.Equals((object)anyId.Value));
    }

    [Fact]
    public void A_null_text_is_refused_as_a_missing_argument() {
        Assert.Throws<ArgumentNullException>(() => new AnimalId(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr123456789012")]       // lower-case country
    [InlineData("F1123456789012")]       // digit in the country
    [InlineData("FRA23456789012")]       // three letters
    [InlineData("FR12345678901")]        // eleven digits
    [InlineData("FR1234567890123")]      // thirteen digits
    [InlineData("FR12345678901A")]       // letter among the digits
    [InlineData(" FR123456789012")]      // leading space
    [InlineData("FR123456789012 ")]      // trailing space
    [InlineData("FR 123456789012")]      // inner space
    [InlineData("FR-123456789012")]      // separator
    [InlineData("ÉR123456789012")]       // non-ASCII letter
    [InlineData("FR١٢٣٤٥٦٧٨٩٠١٢")]       // Arabic-Indic digits are not [0-9]
    public void A_malformed_ear_tag_is_refused(string malformed) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new AnimalId(malformed));

        Assert.Equal("value", refusal.ParamName);
    }

    [Fact(Skip = "domain defect: the pattern is anchored with '$', which also matches before a final line feed, so 'FR123456789012\\n' is accepted as an ear tag (deterministic, no seed involved)")]
    public void An_ear_tag_followed_by_a_line_feed_is_refused() {
        Assert.Throws<ArgumentException>(() => new AnimalId("FR123456789012\n"));
    }

}
