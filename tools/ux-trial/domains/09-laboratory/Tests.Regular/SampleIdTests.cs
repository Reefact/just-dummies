using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class SampleIdTests {

    [Fact]
    public void A_label_of_S_and_ten_digits_is_accepted_and_kept_as_given() {
        // Arrange
        string anyLabel = Any.String().StartingWith("S").Numeric().WithLength(11).Generate();

        // Act
        SampleId sampleId = new SampleId(anyLabel);

        // Assert
        Assert.Equal(anyLabel, sampleId.Value);
        Assert.Equal(anyLabel, sampleId.ToString());
    }

    [Fact]
    public void The_generator_only_draws_ids_the_constructor_accepts() {
        // Act
        SampleId anySampleId = Any.SampleId().Generate();

        // Assert
        Assert.Matches("^S[0-9]{10}$", anySampleId.Value);
    }

    [Fact]
    public void A_null_label_is_refused_as_a_null_argument() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new SampleId(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_label_shorter_than_eleven_characters_is_refused() {
        // Arrange
        string anyTooShort = Any.String().StartingWith("S").Numeric().WithLengthBetween(1, 10).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new SampleId(anyTooShort));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_label_longer_than_eleven_characters_is_refused() {
        // Arrange
        string anyTooLong = Any.String().StartingWith("S").Numeric().WithLengthBetween(12, 60).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SampleId(anyTooLong));
    }

    [Fact]
    public void A_label_that_does_not_start_with_S_is_refused() {
        // Arrange
        string anyDigitsOnly = Any.String().Numeric().WithLength(11).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new SampleId(anyDigitsOnly));
    }

    [Theory]
    [InlineData("")]
    [InlineData("S")]
    [InlineData("s0123456789")]                  // the prefix is an upper-case S
    [InlineData("S012345678a")]                  // a letter among the digits
    [InlineData("S0123 45678")]                  // a space among the digits
    [InlineData("S-123456789")]                  // a sign is not a digit
    [InlineData("S٠١٢٣٤٥٦٧٨٩")]                  // Arabic-Indic digits are digits for char.IsDigit, not ASCII ones
    [InlineData(" S012345678")]
    public void A_label_off_the_format_is_refused(string label) {
        Assert.Throws<ArgumentException>(() => new SampleId(label));
    }

    [Fact]
    public void Two_ids_with_the_same_label_are_equal_and_hash_alike() {
        // Arrange
        string anyLabel = Any.String().StartingWith("S").Numeric().WithLength(11).Generate();

        // Act
        SampleId first  = new SampleId(anyLabel);
        SampleId second = new SampleId(anyLabel);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_ids_with_different_labels_are_different() {
        // Arrange
        SampleId anySampleId      = Any.SampleId().Generate();
        SampleId anyOtherSampleId = Any.SampleId().WithValue(v => v.DifferentFrom(anySampleId.Value)).Generate();

        // Assert
        Assert.False(anySampleId.Equals(anyOtherSampleId));
        Assert.False(anySampleId.Equals((SampleId?)null));
        Assert.False(anySampleId.Equals(anySampleId.Value));
    }

}
