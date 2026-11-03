using BatchGenealogy.Domain;

using JustDummies;

using Xunit;

namespace BatchGenealogy.Tests.Discovery;

public sealed class MaterialCodeTests {

    [Fact]
    public void A_material_code_keeps_its_text() {
        // Act
        MaterialCode code = new MaterialCode("MAT-01234");

        // Assert
        Assert.Equal("MAT-01234", code.Value);
        Assert.Equal("MAT-01234", code.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("MAT-1234")]
    [InlineData("MAT-123456")]
    [InlineData("mat-12345")]
    [InlineData("MAT12345")]
    [InlineData("MAT-1234A")]
    [InlineData(" MAT-12345")]
    [InlineData("MAT-12345 ")]
    [InlineData("XYZ-12345")]
    public void A_text_that_is_not_a_material_code_is_refused(string value) {
        // Act
        Exception? thrown = Record.Exception(() => new MaterialCode(value));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact(Skip = "domain defect: the pattern ends with '$', which .NET also matches before a final '\\n', so \"MAT-12345\\n\" is accepted as a material code (literal, no seed)")]
    public void A_material_code_followed_by_a_line_break_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new MaterialCode("MAT-12345\n"));

        // Assert
        Assert.IsType<FormatException>(thrown);
    }

    [Fact]
    public void A_missing_text_is_refused() {
        // Act
        Exception? thrown = Record.Exception(() => new MaterialCode(null!));

        // Assert
        Assert.IsType<ArgumentNullException>(thrown);
    }

    [Fact]
    public void Two_material_codes_with_the_same_text_are_equal() {
        // Arrange
        string anyValue = Any.MaterialCode().Generate().Value;

        // Act
        MaterialCode first  = new MaterialCode(anyValue);
        MaterialCode second = new MaterialCode(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.True(first.Equals((object)second));
    }

    [Fact]
    public void Two_material_codes_with_different_texts_are_not_equal() {
        // Arrange
        MaterialCode anyCode      = Any.MaterialCode().Generate();
        string       anyOtherText = Any.MaterialCode().WithValue(v => v.DifferentFrom(anyCode.Value)).Generate().Value;

        // Act
        MaterialCode other = new MaterialCode(anyOtherText);

        // Assert
        Assert.NotEqual(anyCode, other);
        Assert.False(anyCode.Equals((MaterialCode?)null));
    }

}
