using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Regular;

public sealed class NetPositionTests {

    [Fact]
    public void A_position_exposes_its_counterparty_net_and_currency() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyNet          = Any.Money().Generate();

        // Act
        NetPosition position = new NetPosition(anyCounterparty, anyNet);

        // Assert
        Assert.Same(anyCounterparty, position.Counterparty);
        Assert.Same(anyNet, position.Net);
        Assert.Equal(anyNet.Currency, position.Currency);
    }

    [Fact]
    public void A_zero_net_is_flat() {
        // Arrange
        Money anyZero = Money.Zero(Any.Currency().Generate());

        // Act
        NetPosition position = new NetPosition(Any.Lei().Generate(), anyZero);

        // Assert
        Assert.True(position.IsFlat);
    }

    [Fact]
    public void A_non_zero_net_is_not_flat_on_either_sign() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyPositive     = Any.Money().WithAmount(a => a.Positive()).Generate();
        Money anyNegative     = Any.Money().WithAmount(a => a.Negative()).Generate();

        // Act / Assert
        Assert.False(new NetPosition(anyCounterparty, anyPositive).IsFlat);
        Assert.False(new NetPosition(anyCounterparty, anyNegative).IsFlat);
    }

    [Fact]
    public void A_position_refuses_a_missing_counterparty_or_net() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyNet          = Any.Money().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new NetPosition(null!, anyNet));
        Assert.Throws<ArgumentNullException>(() => new NetPosition(anyCounterparty, null!));
    }

    [Fact]
    public void Positions_are_equal_by_counterparty_and_net() {
        // Arrange
        NetPosition anyPosition = Any.NetPosition().Generate();

        // Act
        NetPosition same      = new NetPosition(anyPosition.Counterparty, anyPosition.Net);
        NetPosition otherSide = new NetPosition(anyPosition.Counterparty, anyPosition.Net.Negated());

        // Assert
        Assert.Equal(anyPosition, same);
        Assert.Equal(anyPosition.GetHashCode(), same.GetHashCode());
        if (!anyPosition.IsFlat) { Assert.NotEqual(anyPosition, otherSide); }
    }

    [Fact]
    public void A_position_prints_its_counterparty_and_net() {
        // Act
        NetPosition position = new NetPosition(new Lei("5493001KJTIIGC8Y1R12"), new Money(-12.5m, Currency.Usd));

        // Assert
        Assert.Equal("5493001KJTIIGC8Y1R12: -12.50 USD", position.ToString());
    }

}

public sealed class SettlementInstructionTests {

    // 2026-03-18 is a Wednesday.
    private static readonly DateOnly Wednesday = new DateOnly(2026, 3, 18);

    [Fact]
    public void An_instruction_keeps_what_it_was_given() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyAmount       = Any.Money().WithAmount(a => a.Positive()).Generate();

        // Act
        SettlementInstruction instruction = new SettlementInstruction(anyCounterparty, SettlementDirection.Pay, anyAmount, Wednesday);

        // Assert
        Assert.Same(anyCounterparty, instruction.Counterparty);
        Assert.Equal(SettlementDirection.Pay, instruction.Direction);
        Assert.Same(anyAmount, instruction.Amount);
        Assert.Equal(Wednesday, instruction.ValueDate);
    }

    [Fact]
    public void A_zero_amount_is_refused() {
        // Arrange
        Lei                 anyCounterparty = Any.Lei().Generate();
        SettlementDirection anyDirection    = Any.Enum<SettlementDirection>().Generate();
        Money               anyZero         = Money.Zero(Any.Currency().Generate());

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementInstruction(anyCounterparty, anyDirection, anyZero, Wednesday));
    }

    [Fact]
    public void A_negative_amount_is_refused_since_the_direction_carries_the_sign() {
        // Arrange
        Lei                 anyCounterparty = Any.Lei().Generate();
        SettlementDirection anyDirection    = Any.Enum<SettlementDirection>().Generate();
        Money               anyNegative     = Any.Money().WithAmount(a => a.Negative()).Generate();

        // Act
        ArgumentOutOfRangeException refusal = Assert.Throws<ArgumentOutOfRangeException>(
            () => new SettlementInstruction(anyCounterparty, anyDirection, anyNegative, Wednesday));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void An_undefined_direction_is_refused() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyAmount       = Any.Money().WithAmount(a => a.Positive()).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementInstruction(anyCounterparty, (SettlementDirection)9, anyAmount, Wednesday));
    }

    [Theory]
    [InlineData("2026-03-14")]
    [InlineData("2026-03-15")]
    public void A_weekend_value_date_is_refused(string valueDate) {
        // Arrange
        SettlementInstruction anyValid = Any.SettlementInstruction().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SettlementInstruction(anyValid.Counterparty, anyValid.Direction, anyValid.Amount, DateOnly.Parse(valueDate)));
    }

    [Fact]
    public void An_instruction_refuses_a_missing_counterparty_or_amount() {
        // Arrange
        SettlementInstruction anyValid = Any.SettlementInstruction().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new SettlementInstruction(null!, anyValid.Direction, anyValid.Amount, anyValid.ValueDate));
        Assert.Throws<ArgumentNullException>(() => new SettlementInstruction(anyValid.Counterparty, anyValid.Direction, null!, anyValid.ValueDate));
    }

    [Fact]
    public void Instructions_are_equal_when_every_part_is() {
        // Arrange
        SettlementInstruction anyInstruction = Any.SettlementInstruction().Generate();

        // Act
        SettlementInstruction same = new SettlementInstruction(anyInstruction.Counterparty, anyInstruction.Direction, anyInstruction.Amount, anyInstruction.ValueDate);
        SettlementInstruction otherDirection = new SettlementInstruction(anyInstruction.Counterparty,
                                                                         anyInstruction.Direction == SettlementDirection.Pay ? SettlementDirection.Receive : SettlementDirection.Pay,
                                                                         anyInstruction.Amount,
                                                                         anyInstruction.ValueDate);

        // Assert
        Assert.Equal(anyInstruction, same);
        Assert.Equal(anyInstruction.GetHashCode(), same.GetHashCode());
        Assert.NotEqual(anyInstruction, otherDirection);
    }

    [Theory]
    [InlineData(SettlementDirection.Pay, "2026-03-18 Pay 100.00 USD to 5493001KJTIIGC8Y1R12")]
    [InlineData(SettlementDirection.Receive, "2026-03-18 Receive 100.00 USD from 5493001KJTIIGC8Y1R12")]
    public void An_instruction_prints_its_direction_with_to_or_from(SettlementDirection direction, string expected) {
        // Arrange
        SettlementInstruction instruction = new SettlementInstruction(new Lei("5493001KJTIIGC8Y1R12"), direction, new Money(100m, Currency.Usd), Wednesday);

        // Act / Assert
        Assert.Equal(expected, instruction.ToString());
    }

}
