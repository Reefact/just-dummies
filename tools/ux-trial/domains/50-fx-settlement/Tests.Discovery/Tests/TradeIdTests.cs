using Xunit;

namespace FxSettlement.Domain.Tests;

public sealed class TradeIdTests {

    [Theory]
    [InlineData("FX0000048213")]
    [InlineData("FX0000000001")]
    [InlineData("FX9999999999")]
    [InlineData("FX1000000000")]
    public void FX_followed_by_ten_digits_not_all_zero_is_accepted(string value) {
        TradeId id = new TradeId(value);

        Assert.Equal(value, id.Value);
        Assert.Equal(value, id.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("FX")]
    [InlineData("FX000004821")]
    [InlineData("FX00000482133")]
    [InlineData("fx0000048213")]
    [InlineData("FY0000048213")]
    [InlineData("XF0000048213")]
    [InlineData("FX00000482A3")]
    [InlineData("FX-000048213")]
    [InlineData("FX 000048213")]
    [InlineData("0000048213FX")]
    public void Anything_else_than_FX_and_ten_digits_is_refused(string value) {
        Action construct = () => new TradeId(value);

        Assert.Throws<FormatException>(construct);
    }

    [Fact]
    public void Digits_of_other_scripts_are_not_digits() {
        // Arabic-indic digits, which char.IsDigit accepts.
        Action construct = () => new TradeId("FX٠٠٠٠٠٤٨٢١٣");

        Assert.Throws<FormatException>(construct);
    }

    [Fact]
    public void The_all_zero_reference_is_reserved() {
        Action construct = () => new TradeId("FX0000000000");

        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void A_missing_reference_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new TradeId(null!));
    }

    [Fact]
    public void Two_references_are_equal_when_their_values_are_equal() {
        Assert.Equal(new TradeId("FX0000048213"), new TradeId("FX0000048213"));
        Assert.Equal(new TradeId("FX0000048213").GetHashCode(), new TradeId("FX0000048213").GetHashCode());
        Assert.NotEqual(new TradeId("FX0000048213"), new TradeId("FX0000048214"));
    }

}
