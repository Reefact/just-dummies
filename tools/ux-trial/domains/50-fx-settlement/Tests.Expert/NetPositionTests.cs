using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Expert;

public sealed class NetPositionTests {

    [Fact]
    public void A_zero_net_is_flat() {
        // Arrange
        NetPosition anyZeroPosition = Any.NetPosition().WithNet(net => net.WithAmount(0m)).Generate();

        // Act
        bool flat = anyZeroPosition.IsFlat;

        // Assert
        Assert.True(flat);
    }

    [Fact]
    public void A_net_the_bank_receives_is_not_flat() {
        // Arrange
        NetPosition anyReceivedPosition = Any.NetPosition().WithNet(net => net.Positive()).Generate();

        // Assert
        Assert.False(anyReceivedPosition.IsFlat);
    }

    [Fact]
    public void A_net_the_bank_owes_is_not_flat() {
        // Arrange
        NetPosition anyOwedPosition = Any.NetPosition().WithNet(net => net.Between(-1_000m, -1m)).Generate();

        // Assert
        Assert.False(anyOwedPosition.IsFlat);
    }

    [Fact]
    public void A_position_is_in_the_currency_of_its_net() {
        // Arrange
        NetPosition anyKuwaitiPosition = Any.NetPosition().WithNet(net => net.InCurrency(Currency.Kwd)).Generate();

        // Assert
        Assert.Equal(Currency.Kwd, anyKuwaitiPosition.Currency);
    }

    [Fact]
    public void A_position_needs_a_counterparty_and_a_net() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyNet          = Any.Money().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new NetPosition(null!, anyNet));
        Assert.Throws<ArgumentNullException>(() => new NetPosition(anyCounterparty, null!));
    }

    [Fact]
    public void Positions_are_equal_when_counterparty_and_net_are() {
        // Arrange
        Lei anyCounterparty      = Any.Lei().Generate();
        Lei anyOtherCounterparty = Any.Lei().DifferentFrom(anyCounterparty).Generate();

        NetPosition position = new NetPosition(anyCounterparty, new Money(10m, Currency.Eur));

        // Assert
        Assert.Equal(position, new NetPosition(anyCounterparty, new Money(10.00m, Currency.Eur)));
        Assert.NotEqual(position, new NetPosition(anyCounterparty, new Money(11m, Currency.Eur)));
        Assert.NotEqual(position, new NetPosition(anyCounterparty, new Money(10m, Currency.Gbp)));
        Assert.NotEqual(position, new NetPosition(anyOtherCounterparty, new Money(10m, Currency.Eur)));
    }

}
