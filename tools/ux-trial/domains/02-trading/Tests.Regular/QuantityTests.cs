namespace Trading.Tests.Regular;

public sealed class QuantityTests {

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(500)]
    [InlineData(999_999)]
    [InlineData(1_000_000)]
    public void A_quantity_accepts_one_to_a_million_shares(int shares) {
        // Act
        Quantity quantity = new Quantity(shares);

        // Assert
        Assert.Equal(shares, quantity.Shares);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1_000_001)]
    [InlineData(int.MinValue)]
    [InlineData(int.MaxValue)]
    public void A_quantity_refuses_zero_negative_and_more_than_a_million_shares(int shares) {
        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(() => new Quantity(shares));

        // Assert
        Assert.Equal("shares", refusal.ParamName);
        Assert.Equal(shares, refusal.ActualValue);
    }

    [Fact]
    public void The_maximum_is_one_million() {
        Assert.Equal(1_000_000, Quantity.Maximum);
    }

    [Fact]
    public void Two_quantities_of_the_same_shares_are_equal_and_share_a_hash_code() {
        // Arrange
        int anyShares = Any.Quantity().Generate().Shares;

        // Act
        Quantity first  = new Quantity(anyShares);
        Quantity second = new Quantity(anyShares);

        // Assert
        Assert.True(first.Equals(second));
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Two_quantities_of_different_shares_are_different() {
        Assert.False(new Quantity(100).Equals(new Quantity(101)));
    }

    [Fact]
    public void A_quantity_is_not_equal_to_a_bare_int_nor_to_null() {
        // Arrange
        Quantity quantity = new Quantity(100);

        // Assert
        Assert.False(quantity.Equals(100));
        Assert.False(quantity.Equals(null));
    }

}
