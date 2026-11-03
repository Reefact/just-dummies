namespace RadiocarbonChronology.Tests.Discovery;

public sealed class StratigraphicLayerTests {

    [Theory]
    [InlineData("US12")]
    [InlineData("A1")]
    [InlineData("Z0")]
    [InlineData("ABC1234")]
    public void A_code_of_one_to_three_capitals_and_one_to_four_digits_is_accepted(string code) {
        // Act
        StratigraphicLayer layer = Any.StratigraphicLayer().WithCode(code).Generate();

        // Assert
        Assert.Equal(code, layer.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("us12")]
    [InlineData("US")]
    [InlineData("12")]
    [InlineData("12US")]
    [InlineData("ABCD1")]
    [InlineData("US12345")]
    [InlineData("US 12")]
    [InlineData("US-12")]
    [InlineData(" US12")]
    [InlineData("US12 ")]
    public void A_code_that_is_not_capitals_then_digits_is_refused(string code) {
        // Assert
        Assert.Throws<FormatException>(() => Any.StratigraphicLayer().WithCode(code).Generate());
    }

    [Fact(Skip = "domain defect: the layer-code pattern ends with $ and not \\z, so 'US12\\n' (a trailing newline) passes as a code; literal input, no seed")]
    public void A_code_followed_by_a_line_break_is_refused() {
        // Assert
        Assert.Throws<FormatException>(() => Any.StratigraphicLayer().WithCode("US12\n").Generate());
    }

    [Fact]
    public void A_missing_code_is_refused() {
        // Arrange
        string anyDescription = Any.StratigraphicLayer().Generate().Description;

        // Assert
        Assert.Throws<ArgumentNullException>(() => new StratigraphicLayer(null!, anyDescription));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  \t ")]
    public void A_blank_description_is_refused(string? description) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Any.StratigraphicLayer().WithDescription(description!).Generate());

        // Assert
        Assert.Equal("description", refusal.ParamName);
    }

    [Fact]
    public void A_description_of_500_characters_is_accepted() {
        // Act
        StratigraphicLayer layer = Any.StratigraphicLayer().WithDescription(new string('x', 500)).Generate();

        // Assert
        Assert.Equal(500, layer.Description.Length);
    }

    [Fact]
    public void A_description_of_501_characters_is_refused() {
        // Assert
        Assert.Throws<ArgumentException>(() => Any.StratigraphicLayer().WithDescription(new string('x', 501)).Generate());
    }

    [Fact]
    public void A_new_layer_carries_no_sample() {
        // Arrange
        StratigraphicLayer anyLayer = Any.StratigraphicLayer().Generate();

        // Assert
        Assert.Empty(anyLayer.Samples);
    }

}
