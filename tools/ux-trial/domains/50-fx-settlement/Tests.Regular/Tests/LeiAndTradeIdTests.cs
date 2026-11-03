using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Regular;

public sealed class LeiTests {

    [Theory]
    [InlineData("5493001KJTIIGC8Y1R12")]
    [InlineData("529900T8BM49AURSDO55")]
    public void A_well_formed_code_with_correct_check_digits_is_accepted(string value) {
        // Act
        Lei lei = new Lei(value);

        // Assert
        Assert.Equal(value, lei.Value);
        Assert.Equal(value, lei.ToString());
    }

    [Fact]
    public void Altering_the_check_digits_of_a_valid_code_is_refused() {
        // Arrange
        string anyValidCode = Any.Lei().Generate().Value;
        string body         = anyValidCode[..18];
        string checkDigits  = anyValidCode[18..];
        string otherCheck   = Any.StringMatching("[0-9]{2}").DifferentFrom(checkDigits).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new Lei(body + otherCheck));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("5493001KJTIIGC8Y1R1")]
    [InlineData("5493001KJTIIGC8Y1R123")]
    [InlineData("5493001kjtiigc8y1r12")]
    [InlineData("5493001KJTIIGC8Y1RAB")]
    [InlineData("5493001KJTIIGC8Y-R12")]
    [InlineData("")]
    public void A_code_that_is_not_eighteen_alphanumerics_then_two_digits_is_refused(string value) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new Lei(value));
    }

    [Fact]
    public void A_null_code_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new Lei(null!));
    }

    [Fact]
    public void An_arbitrary_lei_is_a_well_formed_code() {
        // Arrange
        string anyCode = Any.Lei().Generate().Value;

        // Act / Assert
        Assert.Matches("^[A-Z0-9]{18}[0-9]{2}$", anyCode);
    }

    [Fact]
    public void Codes_are_equal_by_value() {
        // Arrange
        Lei anyLei = Any.Lei().Generate();

        // Act
        Lei same = new Lei(anyLei.Value);

        // Assert
        Assert.Equal(anyLei, same);
        Assert.Equal(anyLei.GetHashCode(), same.GetHashCode());
        Assert.True(anyLei.Equals((object)same));
        Assert.False(anyLei.Equals((Lei?)null));
    }

    [Fact]
    public void Two_distinct_codes_are_not_equal() {
        // Arrange
        Lei anyLei   = Any.Lei().Generate();
        Lei anyOther = Any.Lei().Generate();

        // Act / Assert (two drawn codes differ but for a chance of 1 in 36^18)
        Assert.NotEqual(anyLei, anyOther);
    }

}

public sealed class TradeIdTests {

    [Fact]
    public void FX_followed_by_ten_digits_is_accepted() {
        // Act
        TradeId id = new TradeId("FX0000048213");

        // Assert
        Assert.Equal("FX0000048213", id.Value);
        Assert.Equal("FX0000048213", id.ToString());
    }

    [Fact]
    public void An_arbitrary_trade_id_is_never_the_reserved_one() {
        // Arrange
        TradeId anyId = Any.TradeId().Generate();

        // Act / Assert
        Assert.NotEqual("FX0000000000", anyId.Value);
    }

    [Fact]
    public void The_reserved_reference_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => new TradeId("FX0000000000"));

        // Assert
        Assert.Equal("value", refusal.ParamName);
    }

    [Theory]
    [InlineData("FX0000000001")]
    [InlineData("FX1000000000")]
    [InlineData("FX9999999999")]
    public void References_next_to_the_reserved_one_are_accepted(string value) {
        // Act / Assert
        Assert.Equal(value, new TradeId(value).Value);
    }

    [Theory]
    [InlineData("FX000004821")]
    [InlineData("FX00000482133")]
    [InlineData("fX0000048213")]
    [InlineData("XX0000048213")]
    [InlineData("FX00000482A3")]
    [InlineData("FX0000048 13")]
    [InlineData("")]
    public void A_reference_that_is_not_FX_and_ten_digits_is_refused(string value) {
        // Act / Assert
        Assert.Throws<FormatException>(() => new TradeId(value));
    }

    [Fact]
    public void A_reference_with_non_ascii_digits_is_refused() {
        // Arrange: Arabic-indic digits are digits for char.IsDigit but not for the desk.
        string anyAsciiTail = Any.StringMatching("[1-9]{9}").Generate();

        // Act / Assert
        Assert.Throws<FormatException>(() => new TradeId("FX١" + anyAsciiTail));
    }

    [Fact]
    public void A_null_reference_is_refused() {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new TradeId(null!));
    }

    [Fact]
    public void References_are_equal_by_value() {
        // Arrange
        TradeId anyId = Any.TradeId().Generate();

        // Act
        TradeId same = new TradeId(anyId.Value);

        // Assert
        Assert.Equal(anyId, same);
        Assert.Equal(anyId.GetHashCode(), same.GetHashCode());
        Assert.False(anyId.Equals((object?)null));
    }

}
