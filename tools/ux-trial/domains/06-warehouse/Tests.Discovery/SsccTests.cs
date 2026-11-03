using JustDummies;

using Warehouse.Domain;

using Xunit;

namespace Warehouse.Tests.Discovery;

public sealed class SsccTests {

    [Fact]
    public void An_sscc_keeps_the_value_it_was_given() {
        // Act
        Sscc sscc = new Sscc("340123450000000017");

        // Assert
        Assert.Equal("340123450000000017", sscc.Value);
        Assert.Equal("340123450000000017", sscc.ToString());
    }

    [Theory]
    [InlineData("340123450000000017", '3')]
    [InlineData("000000000000000000", '0')]
    [InlineData("123456789012345675", '1')]
    public void The_extension_digit_is_the_first_digit(string value, char expected) {
        Sscc sscc = new Sscc(value);

        Assert.Equal(expected, sscc.ExtensionDigit);
    }

    [Fact]
    public void A_null_value_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new Sscc(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData("34012345000000001")]       // 17 digits
    [InlineData("3401234500000000177")]     // 19 digits
    [InlineData("34012345000000001A")]      // a letter
    [InlineData("34012345 000000017")]      // a blank inside
    [InlineData("٣٤٠١٢٣٤٥٠٠٠٠٠٠٠٠١٧")]      // 18 Arabic-Indic digits
    public void A_value_that_is_not_exactly_eighteen_ascii_digits_is_refused(string value) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Sscc(value));

        Assert.Equal("value", exception.ParamName);
    }

    [Theory]
    [InlineData("340123450000000010")]
    [InlineData("340123450000000018")]
    public void A_wrong_check_digit_is_refused(string value) {
        ArgumentException exception = Assert.Throws<ArgumentException>(() => new Sscc(value));

        Assert.Contains(value, exception.Message);
    }

    [Fact]
    public void A_wrong_check_digit_is_refused_whatever_the_body() {
        // Arrange: the body is arbitrary, the digit after it is wrong by construction.
        string anyBody       = Any.StringMatching("[0-9]{17}").Generate();
        int    rightDigit    = Gs1.ComputeCheckDigit(anyBody);
        int    wrongDigit    = (rightDigit + 1) % 10;

        // Act
        Action construct = () => new Sscc(anyBody + wrongDigit);

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void Two_sscc_with_the_same_value_are_equal() {
        // Arrange
        Sscc anySscc     = Any.Sscc().Generate();
        Sscc sameValue   = new Sscc(anySscc.Value);

        // Assert
        Assert.True(anySscc.Equals(sameValue));
        Assert.Equal(anySscc.GetHashCode(), sameValue.GetHashCode());
    }

    [Fact]
    public void Two_sscc_with_different_values_are_not_equal() {
        // Arrange
        Sscc literalSscc  = new Sscc("340123450000000017");
        Sscc otherLiteral = new Sscc("123456789012345675");

        // Assert
        Assert.False(literalSscc.Equals(otherLiteral));
        Assert.False(literalSscc.Equals(null));
        Assert.False(literalSscc.Equals("340123450000000017"));
    }

}
