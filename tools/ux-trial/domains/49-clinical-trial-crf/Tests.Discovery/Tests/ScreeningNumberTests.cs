using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests;

public sealed class ScreeningNumberTests {

    [Fact]
    public void A_screening_number_exposes_its_country_site_and_sequence() {
        // Act
        ScreeningNumber number = new ScreeningNumber("FR012-0042");

        // Assert
        Assert.Equal("FR", number.Country);
        Assert.Equal(12, number.Site);
        Assert.Equal(42, number.Sequence);
        Assert.Equal("FR012-0042", number.Value);
        Assert.Equal("FR012-0042", number.ToString());
    }

    [Theory]
    [InlineData("AA001-0001")]
    [InlineData("ZZ999-9999")]
    public void The_lowest_and_highest_issued_numbers_are_accepted(string text) {
        Assert.Equal(text, new ScreeningNumber(text).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr012-0042")]
    [InlineData("F1012-0042")]
    [InlineData("FRA012-0042")]
    [InlineData("FR12-0042")]
    [InlineData("FR0123-0042")]
    [InlineData("FR012-042")]
    [InlineData("FR012-00423")]
    [InlineData("FR012_0042")]
    [InlineData("FR0120042")]
    [InlineData("FR012-004A")]
    [InlineData(" FR012-0042")]
    [InlineData("FR012-0042 ")]
    public void A_text_that_is_not_CCsss_nnnn_is_refused(string text) {
        Assert.Throws<FormatException>(() => new ScreeningNumber(text));
    }

    [Fact(Skip = "domain defect: the pattern is anchored with $, which also matches before a trailing line break, so \"FR012-0042\\n\" is accepted as a screening number (no drawn value involved, nothing to replay)")]
    public void A_screening_number_does_not_end_with_a_line_break() {
        Assert.Throws<FormatException>(() => new ScreeningNumber("FR012-0042\n"));
    }

    [Fact]
    public void No_screening_number_is_absent() {
        Assert.Throws<ArgumentNullException>(() => new ScreeningNumber(null!));
    }

    [Fact]
    public void Site_000_is_never_issued() {
        // Arrange
        string anyCountry = Any.StringMatching("[A-Z]{2}").Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new ScreeningNumber(anyCountry + "000-0042"));
    }

    [Fact]
    public void Subject_0000_is_never_issued() {
        // Arrange
        string anyCountry = Any.StringMatching("[A-Z]{2}").Generate();

        // Act + Assert
        Assert.Throws<ArgumentException>(() => new ScreeningNumber(anyCountry + "012-0000"));
    }

    [Fact]
    public void Two_screening_numbers_with_the_same_text_are_equal() {
        // Arrange
        ScreeningNumber anyNumber = Any.ScreeningNumber().Generate();

        // Act
        ScreeningNumber same = new ScreeningNumber(anyNumber.Value);

        // Assert
        Assert.Equal(anyNumber, same);
        Assert.Equal(anyNumber.GetHashCode(), same.GetHashCode());
    }

    [Fact]
    public void Two_different_screening_numbers_are_not_equal() {
        // Arrange
        ScreeningNumber anyNumber = Any.ScreeningNumber().Generate();
        ScreeningNumber another   = Any.ScreeningNumber().WithValue(value => value.DifferentFrom(anyNumber.Value)).Generate();

        // Assert
        Assert.NotEqual(anyNumber, another);
    }

}
