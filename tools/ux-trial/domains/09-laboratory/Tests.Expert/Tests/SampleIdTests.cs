namespace Laboratory.Tests;

public sealed class SampleIdTests {

    [Fact]
    public void A_sample_id_is_an_S_followed_by_ten_digits() {
        // Act
        SampleId sampleId = new SampleId("S0123456789");

        // Assert
        Assert.Equal("S0123456789", sampleId.Value);
        Assert.Equal("S0123456789", sampleId.ToString());
    }

    [Fact]
    public void A_missing_sample_id_is_reported_as_missing_not_as_malformed() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new SampleId(null!));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]  // empty
    [InlineData("S123456789")]  // one digit short
    [InlineData("S01234567890")]  // one digit too many
    [InlineData("T0123456789")]  // wrong letter
    [InlineData("s0123456789")]  // the letter is upper case
    [InlineData("00123456789")]  // no letter at all
    [InlineData("S01234567a9")]  // a letter among the digits
    [InlineData("S012345678 ")]  // a trailing space
    [InlineData("S0123456٣٤")]  // Arabic-Indic digits are not ASCII digits
    public void A_malformed_sample_id_is_refused(string malformed) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new SampleId(malformed));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void Two_sample_ids_with_the_same_text_are_equal() {
        // Arrange
        SampleId anyId     = Any.SampleId().Generate();
        SampleId sameTextId = new SampleId(anyId.Value);

        // Assert
        Assert.True(anyId.Equals(sameTextId));
        Assert.Equal(anyId, sameTextId);
        Assert.Equal(anyId.GetHashCode(), sameTextId.GetHashCode());
    }

    [Fact]
    public void Two_sample_ids_with_different_texts_are_not_equal() {
        // Arrange
        SampleId anyId          = Any.SampleId().Generate();
        SampleId anyOtherId     = Any.SampleId().DifferentFrom(anyId).Generate();

        // Assert
        Assert.NotEqual(anyId, anyOtherId);
    }

    [Fact]
    public void A_sample_id_is_never_equal_to_nothing() {
        // Arrange
        SampleId anyId = Any.SampleId().Generate();

        // Assert
        Assert.False(anyId.Equals(null));
        Assert.False(anyId.Equals((object?)null));
    }

}
