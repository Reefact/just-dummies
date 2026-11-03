using JustDummies;

using Xunit;

namespace Laboratory.Domain.Tests;

public sealed class MeasurementUnitTests {

    [Fact]
    public void A_symbol_of_up_to_the_maximum_length_is_accepted_and_kept_as_given() {
        // Arrange
        string anySymbol = Any.String().NotBlank().WithMaxLength(MeasurementUnit.MaxLength).Generate();

        // Act
        MeasurementUnit unit = new MeasurementUnit(anySymbol);

        // Assert
        Assert.Equal(anySymbol, unit.Symbol);
        Assert.Equal(anySymbol, unit.ToString());
    }

    [Fact]
    public void A_symbol_of_exactly_the_maximum_length_is_accepted() {
        // Arrange
        string anySymbolAtTheLimit = Any.String().NotBlank().WithLength(MeasurementUnit.MaxLength).Generate();

        // Act & Assert
        Assert.Equal(anySymbolAtTheLimit, new MeasurementUnit(anySymbolAtTheLimit).Symbol);
    }

    [Fact]
    public void The_maximum_length_is_sixteen() {
        Assert.Equal(16, MeasurementUnit.MaxLength);
    }

    [Fact]
    public void A_symbol_longer_than_the_maximum_is_refused_as_out_of_range() {
        // Arrange
        string anyTooLong = Any.String().Alpha().WithLengthBetween(MeasurementUnit.MaxLength + 1, 200).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new MeasurementUnit(anyTooLong));

        // Assert
        Assert.Equal("symbol", refusal.ParamName);
    }

    [Fact]
    public void A_null_symbol_is_refused_as_a_null_argument() {
        Assert.Throws<ArgumentNullException>(() => new MeasurementUnit(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    [InlineData(" \r\n ")]
    public void A_blank_symbol_is_refused_as_an_argument_error_not_a_null_one(string blank) {
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new MeasurementUnit(blank));

        Assert.Equal("symbol", refusal.ParamName);
    }

    [Fact]
    public void A_blank_symbol_longer_than_the_maximum_is_refused_as_blank() {
        // Arrange
        string anyLongBlank = new string(' ', MeasurementUnit.MaxLength + 1);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new MeasurementUnit(anyLongBlank));
    }

    [Fact]
    public void Two_units_with_the_same_symbol_are_equal_and_hash_alike() {
        // Arrange
        string anySymbol = Any.String().NotBlank().WithMaxLength(MeasurementUnit.MaxLength).Generate();

        // Act
        MeasurementUnit first  = new MeasurementUnit(anySymbol);
        MeasurementUnit second = new MeasurementUnit(anySymbol);

        // Assert
        Assert.True(first.Equals(second));
        Assert.True(first.Equals((object)second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Symbols_that_differ_only_by_case_are_different_units() {
        // Arrange
        string anySymbol = Any.String().Alpha().InLowerCase().WithLengthBetween(1, MeasurementUnit.MaxLength).Generate();

        // Act
        MeasurementUnit lower = new MeasurementUnit(anySymbol);
        MeasurementUnit upper = new MeasurementUnit(anySymbol.ToUpperInvariant());

        // Assert
        Assert.False(lower.Equals(upper));
    }

    [Fact]
    public void A_unit_is_never_equal_to_nothing() {
        Assert.False(Any.MeasurementUnit().Generate().Equals((MeasurementUnit?)null));
    }

}
