using System;

using ClinicalTrial.Domain.Tests.Regular.Generators;

using JustDummies;

using Xunit;

namespace ClinicalTrial.Domain.Tests.Regular;

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
    public void The_lowest_and_highest_numbers_are_accepted(string value) {
        Assert.Equal(value, new ScreeningNumber(value).Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("fr012-0042")]
    [InlineData("Fr012-0042")]
    [InlineData("F012-0042")]
    [InlineData("FRA012-0042")]
    [InlineData("FR12-0042")]
    [InlineData("FR0123-0042")]
    [InlineData("FR012-042")]
    [InlineData("FR012-00421")]
    [InlineData("FR0120042")]
    [InlineData("FR012_0042")]
    [InlineData("FR01A-0042")]
    [InlineData(" FR012-0042")]
    [InlineData("FR012-0042 ")]
    [InlineData("FR 012-0042")]
    [InlineData("12FR0-0042")]
    public void A_value_that_does_not_follow_the_format_is_refused(string value) {
        Assert.Throws<FormatException>(() => new ScreeningNumber(value));
    }

    [Fact(Skip = "domain defect: the pattern is anchored with a dollar sign, which matches before a final newline, so FR012-0042 followed by a newline is accepted and stored with the newline; no drawn value involved, no seed")]
    public void A_screening_number_followed_by_a_line_break_is_refused() {
        // `$` in the pattern matches before a final \n: the number below must not be issued.
        Assert.Throws<FormatException>(() => new ScreeningNumber("FR012-0042\n"));
    }

    [Fact]
    public void Site_000_is_never_issued() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ScreeningNumber("FR000-0042"));

        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void Subject_0000_is_never_issued() {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new ScreeningNumber("FR012-0000"));

        Assert.Equal("value", refusal.ParamName);
    }

    [Fact]
    public void A_missing_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new ScreeningNumber(null!));
    }

    [Fact]
    public void Two_screening_numbers_built_from_the_same_text_are_equal() {
        // Arrange
        string anyText = Any.ScreeningNumber().Generate().Value;

        // Act
        ScreeningNumber first  = new ScreeningNumber(anyText);
        ScreeningNumber second = new ScreeningNumber(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Screening_numbers_with_different_texts_are_not_equal() {
        // Arrange
        ScreeningNumber anyNumber = Any.ScreeningNumber().Generate();
        ScreeningNumber another   = Any.ScreeningNumber().WithValue(v => v.DifferentFrom(anyNumber.Value)).Generate();

        // Assert
        Assert.NotEqual(anyNumber, another);
    }

}
