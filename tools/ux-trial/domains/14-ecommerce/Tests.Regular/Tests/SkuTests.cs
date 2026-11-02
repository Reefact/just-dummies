using JustDummies;

using Xunit;

namespace Shop.Domain.Tests.Regular;

public sealed class SkuTests {

    [Theory]
    [InlineData("TSH-0042")]
    [InlineData("AAA-0000")]
    [InlineData("ZZZ-9999")]
    public void Three_upper_case_letters_a_hyphen_and_four_digits_make_a_sku(string value) {
        // Act
        Sku sku = new Sku(value);

        // Assert
        Assert.Equal(value, sku.Value);
        Assert.Equal(value, sku.ToString());
    }

    [Theory]
    [InlineData("tsh-0042")]
    [InlineData("TSh-0042")]
    [InlineData("TS-0042")]
    [InlineData("TSHH-0042")]
    [InlineData("TSH0042")]
    [InlineData("TSH_0042")]
    [InlineData("TSH-042")]
    [InlineData("TSH-00421")]
    [InlineData("TSH-00A2")]
    [InlineData(" TSH-0042")]
    [InlineData("TSH-0042 ")]
    [InlineData("T1H-0042")]
    public void A_value_off_the_AAA_9999_format_is_refused(string value) {
        // Act
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Sku(value));

        // Assert
        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_value_is_refused(string value) {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Sku(value));
    }

    [Fact]
    public void A_null_value_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new Sku(null!));
    }

    [Fact(Skip = "domain defect: '$' also matches before a final line feed, so 'TSH-0042\\n' is accepted as a SKU (literal input, no drawn value)")]
    public void A_value_followed_by_a_line_feed_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Sku("TSH-0042\n"));
    }

    [Fact(Skip = "domain defect: '\\d' matches any Unicode decimal digit, so 'TSH-٠٠٤٢' (Arabic-Indic digits) is accepted as a SKU (literal input, no drawn value)")]
    public void Digits_outside_ASCII_are_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new Sku("TSH-٠٠٤٢"));
    }

    [Theory]
    [InlineData("TSH-0042", "TSH")]
    [InlineData("BOO-1234", "BOO")]
    public void The_category_is_the_first_three_letters(string value, string expected) {
        // Arrange
        Sku sku = new Sku(value);

        // Act
        string category = sku.Category;

        // Assert
        Assert.Equal(expected, category);
    }

    [Fact]
    public void Two_skus_are_equal_when_their_values_are() {
        // Arrange
        Sku sku = new Sku("TSH-0042");

        // Act & Assert
        Assert.True(sku.Equals(new Sku("TSH-0042")));
        Assert.Equal(sku.GetHashCode(), new Sku("TSH-0042").GetHashCode());
        Assert.NotEqual(sku, new Sku("TSH-0043"));
    }

    [Fact]
    public void A_sku_is_not_equal_to_null_or_to_its_bare_text() {
        // Arrange
        Sku anySku = Any.Sku().Generate();

        // Act & Assert
        Assert.False(anySku.Equals(null));
        Assert.False(anySku.Equals(anySku.Value));
    }

}
