using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Expert;

public sealed class TradeIdTests {

    [Theory]
    [InlineData("FX0000048213")]
    [InlineData("FX0000000001")]
    [InlineData("FX9999999999")]
    public void FX_followed_by_ten_digits_is_a_trade_reference(string reference) {
        // Act
        TradeId id = new TradeId(reference);

        // Assert
        Assert.Equal(reference, id.Value);
        Assert.Equal(reference, id.ToString());
    }

    [Fact]
    public void The_all_zero_reference_is_reserved() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradeId("FX0000000000"));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("FX000004821")]
    [InlineData("FX00000482133")]
    [InlineData("fx0000048213")]
    [InlineData("XF0000048213")]
    [InlineData("FX00000482A3")]
    [InlineData("FX-000048213")]
    [InlineData("FX 000048213")]
    [InlineData("GB0000048213")]
    public void Anything_else_is_not_a_trade_reference(string malformed) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new TradeId(malformed));
    }

    [Fact]
    public void Digits_of_other_scripts_are_not_digits() {
        // Arrange: ARABIC-INDIC DIGIT ONE is a Unicode digit but not an ASCII one
        string arabicIndicDigitOne = "FX000004821١";

        // Act / Assert
        Assert.Throws<FormatException>(() => new TradeId(arabicIndicDigitOne));
    }

    [Fact]
    public void A_missing_reference_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new TradeId(null!));
    }

    [Fact]
    public void Trade_references_are_equal_when_their_values_are() {
        // Arrange
        string anyValue = Any.TradeId().Generate().Value;

        // Act
        TradeId first  = new TradeId(anyValue);
        TradeId second = new TradeId(anyValue);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Different_values_are_different_references() {
        // Assert
        Assert.NotEqual(new TradeId("FX0000000001"), new TradeId("FX0000000002"));
    }

}
