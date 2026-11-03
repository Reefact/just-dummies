namespace Laboratory.Tests;

public sealed class MeasurementUnitTests {

    [Fact]
    public void A_unit_exposes_the_symbol_it_was_given() {
        // Act
        MeasurementUnit unit = new MeasurementUnit("mmol/L");

        // Assert
        Assert.Equal("mmol/L", unit.Symbol);
        Assert.Equal("mmol/L", unit.ToString());
    }

    [Fact]
    public void A_missing_symbol_is_reported_as_missing() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => new MeasurementUnit(null!));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData("   \r\n ")]
    public void A_blank_symbol_is_refused(string blank) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new MeasurementUnit(blank));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
    }

    [Fact]
    public void A_symbol_of_the_maximum_length_is_accepted() {
        // Arrange
        string anyLongestSymbol = Any.String().NotBlank().WithLength(MeasurementUnit.MaxLength).Generate();

        // Act
        MeasurementUnit unit = new MeasurementUnit(anyLongestSymbol);

        // Assert
        Assert.Equal(MeasurementUnit.MaxLength, unit.Symbol.Length);
    }

    [Fact]
    public void A_symbol_one_character_beyond_the_maximum_is_refused() {
        // Arrange
        string anyTooLongSymbol = Any.String().NotBlank().WithLength(MeasurementUnit.MaxLength + 1).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new MeasurementUnit(anyTooLongSymbol));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
    }

    [Fact]
    public void Two_units_with_the_same_symbol_are_equal() {
        // Arrange
        MeasurementUnit anyUnit      = Any.MeasurementUnit().Generate();
        MeasurementUnit sameSymbolUnit = new MeasurementUnit(anyUnit.Symbol);

        // Assert
        Assert.True(anyUnit.Equals(sameSymbolUnit));
        Assert.Equal(anyUnit.GetHashCode(), sameSymbolUnit.GetHashCode());
    }

    [Fact]
    public void Symbols_differing_only_by_case_are_different_units() {
        // mmol/L and mmol/l are both written in the wild, and they are not the same unit to this model.
        MeasurementUnit upper = new MeasurementUnit("mmol/L");
        MeasurementUnit lower = new MeasurementUnit("mmol/l");

        Assert.False(upper.Equals(lower));
    }

    [Fact]
    public void Two_units_with_different_symbols_are_not_equal() {
        // Arrange
        MeasurementUnit anyUnit      = Any.MeasurementUnit().Generate();
        MeasurementUnit anyOtherUnit = Any.MeasurementUnit().DifferentFrom(anyUnit).Generate();

        // Assert
        Assert.False(anyUnit.Equals(anyOtherUnit));
    }

}
