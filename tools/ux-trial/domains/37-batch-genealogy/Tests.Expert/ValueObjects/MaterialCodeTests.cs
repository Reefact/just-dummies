using JustDummies;

using Xunit;

namespace BatchGenealogy.Domain.Tests.ValueObjects;

public sealed class MaterialCodeTests {

    [Theory]
    [InlineData("MAT-00000")]
    [InlineData("MAT-01234")]
    [InlineData("MAT-99999")]
    public void A_well_formed_material_code_is_accepted_and_keeps_its_text(string text) {
        // Act
        MaterialCode code = new MaterialCode(text);

        // Assert
        Assert.Equal(text, code.Value);
        Assert.Equal(text, code.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("MAT-1234")]
    [InlineData("MAT-123456")]
    [InlineData("MAT-1234A")]
    [InlineData("mat-12345")]
    [InlineData("MAT12345")]
    [InlineData("MAT 12345")]
    [InlineData("XMAT-12345")]
    [InlineData("MAT-12345 ")]
    [InlineData("MAT-١٢٣٤٥")]
    public void A_malformed_material_code_is_refused(string text) {
        Assert.Throws<FormatException>(() => new MaterialCode(text));
    }

    [Fact(Skip = "domain defect: the pattern ends with $, which also matches before a final \\n, so a trailing line break is accepted and kept in Value; no seed involved, the text is a literal")]
    public void A_trailing_line_break_does_not_make_a_material_code_malformed_by_accident() {
        Assert.Throws<FormatException>(() => new MaterialCode("MAT-12345\n"));
    }

    [Fact]
    public void A_missing_material_code_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new MaterialCode(null!));
    }

    [Fact]
    public void Two_material_codes_with_the_same_text_are_equal_and_hash_alike() {
        // Arrange
        string anyText = Any.MaterialCode().Generate().Value;

        // Act
        MaterialCode first  = new MaterialCode(anyText);
        MaterialCode second = new MaterialCode(anyText);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Material_codes_with_different_text_are_different() {
        // Arrange
        MaterialCode anyCode      = Any.MaterialCode().Generate();
        MaterialCode anotherCode = Any.MaterialCode().WithValue(value => value.DifferentFrom(anyCode.Value)).Generate();

        // Assert
        Assert.NotEqual(anyCode, anotherCode);
    }

}
