using JustDummies;

using Xunit;

namespace FxSettlement.Domain.Tests.Expert;

public sealed class SettlementInstructionTests {

    [Fact]
    public void An_instruction_moves_a_positive_amount_on_a_business_day() {
        // Arrange
        Lei counterparty = new Lei("529900T8BM49AURSDO55");

        // Act
        SettlementInstruction instruction = new SettlementInstruction(counterparty, SettlementDirection.Pay, new Money(1_250m, Currency.Eur), Week.Wednesday);

        // Assert
        Assert.Equal(counterparty, instruction.Counterparty);
        Assert.Equal(SettlementDirection.Pay, instruction.Direction);
        Assert.Equal(new Money(1_250m, Currency.Eur), instruction.Amount);
        Assert.Equal(Week.Wednesday, instruction.ValueDate);
    }

    [Fact]
    public void The_smallest_positive_amount_is_enough() {
        // Arrange
        Lei       anyCounterparty = Any.Lei().Generate();
        Currency  anyCurrency     = Any.Currency().Generate();
        Money     oneMinorUnit    = new Money(anyCurrency.SmallestUnit, anyCurrency);

        // Act
        Exception? refusal = Record.Exception(() => new SettlementInstruction(anyCounterparty, SettlementDirection.Receive, oneMinorUnit, Week.Monday));

        // Assert
        Assert.Null(refusal);
    }

    [Fact]
    public void A_zero_amount_is_refused() {
        // Arrange
        Lei      anyCounterparty = Any.Lei().Generate();
        Currency anyCurrency     = Any.Currency().Generate();
        DateOnly anyBusinessDay  = Any.BusinessDay().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementInstruction(anyCounterparty, SettlementDirection.Pay, Money.Zero(anyCurrency), anyBusinessDay));
    }

    [Fact]
    public void A_negative_amount_is_refused_because_the_direction_carries_the_sign() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyDebit        = new AnyMoney().Between(-1_000m, -1m).Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementInstruction(anyCounterparty, SettlementDirection.Receive, anyDebit, Week.Monday));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(-1)]
    public void A_direction_the_desk_does_not_know_is_refused(int unknown) {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyAmount       = new AnyMoney().Positive().Generate();

        // Act / Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => new SettlementInstruction(anyCounterparty, (SettlementDirection)unknown, anyAmount, Week.Monday));
    }

    [Fact]
    public void Nothing_settles_on_a_weekend() {
        // Arrange
        Lei      anyCounterparty = Any.Lei().Generate();
        Money    anyAmount       = new AnyMoney().Positive().Generate();
        DateOnly anyWeekendDay   = Any.WeekendDay().Generate();

        // Act / Assert
        Assert.Throws<ArgumentException>(() => new SettlementInstruction(anyCounterparty, SettlementDirection.Pay, anyAmount, anyWeekendDay));
    }

    [Fact]
    public void An_instruction_needs_a_counterparty_and_an_amount() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money anyAmount       = new AnyMoney().Positive().Generate();

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new SettlementInstruction(null!, SettlementDirection.Pay, anyAmount, Week.Monday));
        Assert.Throws<ArgumentNullException>(() => new SettlementInstruction(anyCounterparty, SettlementDirection.Pay, null!, Week.Monday));
    }

    [Fact]
    public void Instructions_are_equal_when_every_part_is() {
        // Arrange
        Lei   anyCounterparty = Any.Lei().Generate();
        Money amount          = new Money(10m, Currency.Chf);

        SettlementInstruction instruction = new SettlementInstruction(anyCounterparty, SettlementDirection.Pay, amount, Week.Monday);

        // Assert
        Assert.Equal(instruction, new SettlementInstruction(anyCounterparty, SettlementDirection.Pay, new Money(10.00m, Currency.Chf), Week.Monday));
        Assert.NotEqual(instruction, new SettlementInstruction(anyCounterparty, SettlementDirection.Receive, amount, Week.Monday));
        Assert.NotEqual(instruction, new SettlementInstruction(anyCounterparty, SettlementDirection.Pay, new Money(11m, Currency.Chf), Week.Monday));
        Assert.NotEqual(instruction, new SettlementInstruction(anyCounterparty, SettlementDirection.Pay, amount, Week.Tuesday));
        Assert.NotEqual(instruction, new SettlementInstruction(Any.Lei().DifferentFrom(anyCounterparty).Generate(), SettlementDirection.Pay, amount, Week.Monday));
    }

    [Theory]
    [InlineData(SettlementDirection.Pay, "2025-03-03 Pay 1250.00 EUR to 529900T8BM49AURSDO55")]
    [InlineData(SettlementDirection.Receive, "2025-03-03 Receive 1250.00 EUR from 529900T8BM49AURSDO55")]
    public void An_instruction_reads_as_the_payment_it_is(SettlementDirection direction, string expected) {
        // Arrange
        SettlementInstruction instruction = new SettlementInstruction(new Lei("529900T8BM49AURSDO55"), direction, new Money(1_250m, Currency.Eur), Week.Monday);

        // Act
        string text = instruction.ToString();

        // Assert
        Assert.Equal(expected, text);
    }

}
