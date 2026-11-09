using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests;

public sealed class NetPositionTests {

    [Fact]
    public void A_zero_net_is_flat() {
        // Arrange
        Lei      anyCounterparty = Any.Lei().Generate();
        Currency anyCurrency     = Any.Currency().Generate();

        // Act
        NetPosition position = new NetPosition(anyCounterparty, Money.Zero(anyCurrency));

        // Assert
        Assert.True(position.IsFlat);
    }

    [Fact]
    public void A_non_zero_net_is_not_flat_whichever_its_sign() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyPositive     = Any.Money().WithAmount(a => a.Positive()).Generate();
        Money anyNegative     = anyPositive.Negated();

        // Assert
        Assert.False(new NetPosition(anyCounterparty, anyPositive).IsFlat);
        Assert.False(new NetPosition(anyCounterparty, anyNegative).IsFlat);
    }

    [Fact]
    public void The_currency_of_a_position_is_the_currency_of_its_net() {
        Assert.Equal(Currency.Chf, new NetPosition(Fixtures.LeiFirst, new Money(12.50m, Currency.Chf)).Currency);
    }

    [Fact]
    public void A_position_without_counterparty_or_net_is_refused() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyNet          = Any.Money().Generate();

        // Assert
        Assert.Throws<ArgumentNullException>(() => new NetPosition(null!, anyNet));
        Assert.Throws<ArgumentNullException>(() => new NetPosition(anyCounterparty, null!));
    }

    [Fact]
    public void Two_positions_are_equal_when_counterparty_and_net_are_equal() {
        NetPosition reference = new NetPosition(Fixtures.LeiFirst, Fixtures.Usd(10m));

        Assert.Equal(reference, new NetPosition(Fixtures.LeiFirst, Fixtures.Usd(10.00m)));
        Assert.Equal(reference.GetHashCode(), new NetPosition(Fixtures.LeiFirst, Fixtures.Usd(10.00m)).GetHashCode());
        Assert.NotEqual(reference, new NetPosition(Fixtures.LeiSecond, Fixtures.Usd(10m)));
        Assert.NotEqual(reference, new NetPosition(Fixtures.LeiFirst, Fixtures.Usd(-10m)));
    }

    [Fact]
    public void A_position_reads_as_counterparty_then_net() {
        Assert.Equal("529900T8BM49AURSDO55: -12.50 CHF", new NetPosition(Fixtures.LeiFirst, new Money(-12.50m, Currency.Chf)).ToString());
    }

}
