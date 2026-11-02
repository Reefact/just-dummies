using JustDummies;

using Xunit;

namespace Shop.Domain.Tests;

public sealed class SkuTests {

    [Theory]
    [InlineData("TSH-0042")]
    [InlineData("AAA-0000")]
    [InlineData("ZZZ-9999")]
    public void Three_capital_letters_a_hyphen_and_four_digits_make_a_sku(string value) {
        // Act
        Sku sku = new Sku(value);

        // Assert
        Assert.Equal(value, sku.Value);
        Assert.Equal(value, sku.ToString());
    }

    [Fact]
    public void Its_category_is_its_three_letters() {
        // Arrange
        Sku sku = new Sku("TSH-0042");

        // Act
        string category = sku.Category;

        // Assert
        Assert.Equal("TSH", category);
    }

    [Fact]
    public void A_missing_sku_is_refused_as_null() {
        // Act / Assert
        ArgumentNullException error = Assert.Throws<ArgumentNullException>(() => new Sku(null!));
        Assert.Equal("value", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void A_blank_sku_is_refused(string value) {
        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Sku(value));
        Assert.Equal("value", error.ParamName);
    }

    [Theory]
    [InlineData("tsh-0042")]        // lower case
    [InlineData("TSh-0042")]        // one lower-case letter
    [InlineData("TSH0042")]         // no hyphen
    [InlineData("TSH_0042")]        // wrong separator
    [InlineData("TS-0042")]         // two letters
    [InlineData("TSHX-0042")]       // four letters
    [InlineData("TSH-042")]         // three digits
    [InlineData("TSH-00423")]       // five digits
    [InlineData("TSH-004A")]        // a letter among the digits
    [InlineData(" TSH-0042")]       // leading space
    [InlineData("TSH-0042 ")]       // trailing space
    [InlineData("T1H-0042")]        // a digit among the letters
    public void Anything_else_is_refused_as_a_malformed_sku(string value) {
        // Act / Assert
        ArgumentException error = Assert.Throws<ArgumentException>(() => new Sku(value));
        Assert.Equal("value", error.ParamName);
        Assert.Contains($"'{value}'", error.Message);
    }

    [Fact(Skip = "domain defect: a SKU with a trailing line feed is accepted (Regex '$' matches before a final \\n); no draw involved, literal case")]
    public void A_sku_followed_by_a_line_feed_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Sku("TSH-0042\n"));
    }

    [Fact(Skip = "domain defect: a SKU whose digits are not ASCII digits is accepted (\\d matches any Unicode decimal digit); no draw involved, literal case")]
    public void A_sku_with_non_ascii_digits_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentException>(() => new Sku("TSH-٠٠٤٢"));
    }

    [Fact]
    public void Two_skus_of_the_same_value_are_equal() {
        // Arrange
        Sku anySku  = Any.Sku().Generate();
        Sku sameSku = new Sku(anySku.Value);

        // Act
        bool areEqual = anySku.Equals(sameSku);

        // Assert
        Assert.True(areEqual);
        Assert.Equal(anySku.GetHashCode(), sameSku.GetHashCode());
    }

    [Fact]
    public void Two_skus_of_different_values_are_not_equal() {
        // Arrange
        Sku anySku      = Any.Sku().Generate();
        Sku anyOtherSku = Any.Sku().WithValue(v => v.DifferentFrom(anySku.Value)).Generate();

        // Act
        bool areEqual = anySku.Equals(anyOtherSku);

        // Assert
        Assert.False(areEqual);
    }

}
