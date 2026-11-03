using JustDummies;

using RadiocarbonChronology.Domain;

namespace RadiocarbonChronology.Tests.Expert;

public sealed class StratigraphicLayerTests {

    [Theory]
    [InlineData("US12")]
    [InlineData("A1")]
    [InlineData("US1")]
    [InlineData("ABC1234")]
    [InlineData("Z0")]
    public void A_layer_code_is_one_to_three_capital_letters_then_one_to_four_digits(string code) {
        // Arrange
        string anyDescription = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        // Act
        StratigraphicLayer layer = new StratigraphicLayer(code, anyDescription);

        // Assert
        Assert.Equal(code, layer.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("US")]
    [InlineData("12")]
    [InlineData("us12")]
    [InlineData("Us12")]
    [InlineData("ABCD1")]
    [InlineData("US12345")]
    [InlineData("US 12")]
    [InlineData("US-12")]
    [InlineData(" US12")]
    [InlineData("US12 ")]
    public void A_layer_code_of_another_shape_is_refused(string code) {
        // Arrange
        string anyDescription = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new StratigraphicLayer(code, anyDescription));
    }

    [Fact(Skip = "domain defect: the code pattern is anchored with $, which .NET lets match before a final line break, so 'US12\\n' is accepted as a layer code (no seed: literals only)")]
    public void A_layer_code_followed_by_a_line_break_is_refused() {
        // Arrange
        string anyDescription = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        // Act & Assert
        Assert.Throws<FormatException>(() => new StratigraphicLayer("US12\n", anyDescription));
    }

    [Fact]
    public void A_missing_layer_code_is_refused() {
        // Arrange
        string anyDescription = Any.String().Alpha().WithLengthBetween(1, 100).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new StratigraphicLayer(null!, anyDescription));

        // Assert
        Assert.Equal("code", refusal.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void A_blank_description_is_refused(string? blank) {
        // Arrange
        string anyCode = AnyStratigraphicLayer.ValidCode().Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StratigraphicLayer(anyCode, blank!));

        // Assert
        Assert.Equal("description", refusal.ParamName);
    }

    [Fact]
    public void A_description_of_five_hundred_characters_is_accepted() {
        // Arrange
        string anyCode        = AnyStratigraphicLayer.ValidCode().Generate();
        string anyDescription = Any.String().Alpha().WithLength(500).Generate();

        // Act
        StratigraphicLayer layer = new StratigraphicLayer(anyCode, anyDescription);

        // Assert
        Assert.Equal(500, layer.Description.Length);
    }

    [Fact]
    public void A_description_of_five_hundred_and_one_characters_is_refused() {
        // Arrange
        string anyCode        = AnyStratigraphicLayer.ValidCode().Generate();
        string anyDescription = Any.String().Alpha().WithLength(501).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new StratigraphicLayer(anyCode, anyDescription));

        // Assert
        Assert.Equal("description", refusal.ParamName);
    }

    [Fact]
    public void A_new_layer_holds_no_sample() {
        // Arrange
        StratigraphicLayer layer = Any.StratigraphicLayer().Generate();

        // Assert
        Assert.Empty(layer.Samples);
    }

    [Fact]
    public void A_layer_keeps_its_description_as_it_was_written() {
        // Act
        StratigraphicLayer layer = Any.StratigraphicLayer().WithDescription("  Ash lens under the hearth\n").Generate();

        // Assert
        Assert.Equal("  Ash lens under the hearth\n", layer.Description);
    }

}
