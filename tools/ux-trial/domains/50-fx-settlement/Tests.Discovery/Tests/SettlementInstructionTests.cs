using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests;

public sealed class SettlementInstructionTests {

    [Fact]
    public void An_instruction_keeps_what_it_was_built_from() {
        // Act
        SettlementInstruction instruction = new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Pay, Fixtures.Eur(250.75m), Fixtures.Wednesday);

        // Assert
        Assert.Equal(Fixtures.LeiFirst, instruction.Counterparty);
        Assert.Equal(SettlementDirection.Pay, instruction.Direction);
        Assert.Equal(Fixtures.Eur(250.75m), instruction.Amount);
        Assert.Equal(Fixtures.Wednesday, instruction.ValueDate);
    }

    [Fact]
    public void A_zero_amount_is_refused() {
        // Arrange
        Currency anyCurrency     = Any.Currency().Generate();
        Lei      anyCounterparty = Any.Lei().Generate();
        SettlementDirection anyDirection = Any.Enum<SettlementDirection>().Generate();
        DateOnly anyValueDate    = BusinessDays.Any().Generate();

        // Act
        Action construct = () => new SettlementInstruction(anyCounterparty, anyDirection, Money.Zero(anyCurrency), anyValueDate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void A_negative_amount_is_refused_since_the_direction_carries_the_sign() {
        // Arrange
        Money               anyNegativeAmount = Any.Money().WithAmount(a => a.Negative()).Generate();
        Lei                 anyCounterparty   = Any.Lei().Generate();
        SettlementDirection anyDirection      = Any.Enum<SettlementDirection>().Generate();
        DateOnly            anyValueDate      = BusinessDays.Any().Generate();

        // Act
        Action construct = () => new SettlementInstruction(anyCounterparty, anyDirection, anyNegativeAmount, anyValueDate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Fact]
    public void An_undefined_direction_is_refused() {
        // Arrange
        Lei      anyCounterparty = Any.Lei().Generate();
        Money    anyAmount       = Any.Money().WithAmount(a => a.Positive()).Generate();
        DateOnly anyValueDate    = BusinessDays.Any().Generate();

        // Act
        Action construct = () => new SettlementInstruction(anyCounterparty, (SettlementDirection)99, anyAmount, anyValueDate);

        // Assert
        Assert.Throws<ArgumentOutOfRangeException>(construct);
    }

    [Theory]
    [InlineData("2025-03-15")]
    [InlineData("2025-03-16")]
    public void A_weekend_value_date_is_refused(string valueDate) {
        // Arrange
        Lei                 anyCounterparty = Any.Lei().Generate();
        SettlementDirection anyDirection    = Any.Enum<SettlementDirection>().Generate();
        Money               anyAmount       = Any.Money().WithAmount(a => a.Positive()).Generate();

        // Act
        Action construct = () => new SettlementInstruction(anyCounterparty, anyDirection, anyAmount, DateOnly.Parse(valueDate));

        // Assert
        Assert.Throws<ArgumentException>(construct);
    }

    [Fact]
    public void The_smallest_unit_of_a_currency_is_a_positive_amount() {
        // Arrange
        Lei                 anyCounterparty = Any.Lei().Generate();
        SettlementDirection anyDirection    = Any.Enum<SettlementDirection>().Generate();
        DateOnly            anyValueDate    = BusinessDays.Any().Generate();

        // Act
        SettlementInstruction instruction = new SettlementInstruction(anyCounterparty, anyDirection, new Money(0.001m, Currency.Kwd), anyValueDate);

        // Assert
        Assert.Equal(0.001m, instruction.Amount.Amount);
    }

    [Fact]
    public void Two_instructions_are_equal_when_all_four_parts_are_equal() {
        SettlementInstruction reference = new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Pay, Fixtures.Eur(250.75m), Fixtures.Wednesday);

        Assert.Equal(reference, new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Pay, Fixtures.Eur(250.75m), Fixtures.Wednesday));
        Assert.Equal(reference.GetHashCode(), new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Pay, Fixtures.Eur(250.75m), Fixtures.Wednesday).GetHashCode());
        Assert.NotEqual(reference, new SettlementInstruction(Fixtures.LeiSecond, SettlementDirection.Pay, Fixtures.Eur(250.75m), Fixtures.Wednesday));
        Assert.NotEqual(reference, new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Receive, Fixtures.Eur(250.75m), Fixtures.Wednesday));
        Assert.NotEqual(reference, new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Pay, Fixtures.Eur(250.76m), Fixtures.Wednesday));
        Assert.NotEqual(reference, new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Pay, Fixtures.Eur(250.75m), Fixtures.Thursday));
    }

    [Fact]
    public void An_instruction_reads_as_date_direction_amount_and_counterparty() {
        SettlementInstruction pay     = new SettlementInstruction(Fixtures.LeiFirst, SettlementDirection.Pay, Fixtures.Eur(250.75m), Fixtures.Wednesday);
        SettlementInstruction receive = new SettlementInstruction(Fixtures.LeiSecond, SettlementDirection.Receive, Fixtures.Jpy(1000m), Fixtures.Thursday);

        Assert.Equal("2025-03-12 Pay 250.75 EUR to 529900T8BM49AURSDO55", pay.ToString());
        Assert.Equal("2025-03-13 Receive 1000 JPY from 5493001KJTIIGC8Y1R12", receive.ToString());
    }

}
