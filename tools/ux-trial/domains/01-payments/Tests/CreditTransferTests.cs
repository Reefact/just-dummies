using JustDummies;
using JustDummies.Xunit;
using Payments.Domain;
using Xunit;

namespace Payments.Tests;

[Reproducible]
public sealed class CreditTransferTests {

    private static readonly DateOnly Today = new(2026, 10, 27);

    [Fact]
    public void Submitting_a_draft_due_today_or_later_makes_it_submitted() {
        // Arrange
        CreditTransfer anyDueTransfer = Arbitrary.CreditTransfer()
                                                 .WithRequestedExecutionDate(d => d.AfterOrEqualTo(Today))
                                                 .Generate();

        // Act
        anyDueTransfer.Submit(Today);

        // Assert
        Assert.Equal(TransferStatus.Submitted, anyDueTransfer.Status);
    }

    [Fact]
    public void Submitting_a_transfer_whose_execution_date_has_passed_is_refused() {
        // Arrange
        CreditTransfer anyOverdueTransfer = Arbitrary.CreditTransfer()
                                                     .WithRequestedExecutionDate(d => d.Before(Today))
                                                     .Generate();

        // Act
        Action submit = () => anyOverdueTransfer.Submit(Today);

        // Assert
        Assert.Throws<InvalidOperationException>(submit);
        Assert.Equal(TransferStatus.Draft, anyOverdueTransfer.Status);
    }

    [Fact]
    public void A_submitted_transfer_cannot_be_submitted_again() {
        // Arrange
        CreditTransfer anyTransfer = Arbitrary.CreditTransfer().WithRequestedExecutionDate(d => d.AfterOrEqualTo(Today)).Generate();
        anyTransfer.Submit(Today);

        // Act
        Action submitAgain = () => anyTransfer.Submit(Today);

        // Assert
        Assert.Throws<InvalidOperationException>(submitAgain);
    }

    [Fact]
    public void A_draft_cannot_be_rejected() {
        // Arrange
        CreditTransfer anyDraft = Arbitrary.CreditTransfer().Generate();

        // Act
        Action reject = anyDraft.Reject;

        // Assert
        Assert.Throws<InvalidOperationException>(reject);
    }

    [Fact]
    public void A_submitted_transfer_can_be_executed() {
        // Arrange
        CreditTransfer anyTransfer = Arbitrary.CreditTransfer().WithRequestedExecutionDate(d => d.AfterOrEqualTo(Today)).Generate();
        anyTransfer.Submit(Today);

        // Act
        anyTransfer.MarkExecuted();

        // Assert
        Assert.Equal(TransferStatus.Executed, anyTransfer.Status);
    }

    [Fact]
    public void A_transfer_between_two_countries_is_cross_border() {
        // Arrange
        CreditTransfer anyGermanToFrenchTransfer = Arbitrary.CreditTransfer()
                                                            .WithDebtor(p => p.InCountry("DE"))
                                                            .WithCreditor(p => p.InCountry("FR"))
                                                            .Generate();

        // Act
        bool crossBorder = anyGermanToFrenchTransfer.IsCrossBorder;

        // Assert
        Assert.True(crossBorder);
    }

    [Fact]
    public void A_transfer_inside_one_country_is_domestic() {
        // Arrange
        CreditTransfer anyDutchTransfer = Arbitrary.CreditTransfer()
                                                   .WithDebtor(p => p.InCountry("NL"))
                                                   .WithCreditor(p => p.InCountry("NL"))
                                                   .Generate();

        // Act
        bool crossBorder = anyDutchTransfer.IsCrossBorder;

        // Assert
        Assert.False(crossBorder);
    }

    [Fact]
    public void A_transfer_in_another_currency_than_the_euro_is_refused() {
        // Arrange
        Money anyNonEuroAmount = Arbitrary.Money().WithCurrency(c => c.DifferentFrom(Currency.EUR)).Generate();

        // Act
        Func<CreditTransfer> create = () => Arbitrary.CreditTransfer().WithAmount(anyNonEuroAmount).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(create);
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void An_instant_transfer_above_the_ceiling_is_refused() {
        // Arrange
        Money justAboveTheCeiling = new(100_000.01m, Currency.EUR);

        // Act
        Func<CreditTransfer> create = () => Arbitrary.CreditTransfer()
                                                     .WithPriority(TransferPriority.Instant)
                                                     .WithAmount(justAboveTheCeiling)
                                                     .Generate();

        // Assert
        Assert.Throws<ArgumentException>(create);
    }

    [Fact]
    public void A_transfer_to_the_debtor_own_account_is_refused() {
        // Arrange
        Party anyParty = Arbitrary.Party().Generate();

        // Act
        Func<CreditTransfer> create = () => Arbitrary.CreditTransfer().WithDebtor(anyParty).WithCreditor(anyParty).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(create);
        Assert.Equal("creditor", refusal.ParamName);
    }

    [Fact]
    public void A_transfer_requested_on_a_saturday_is_refused() {
        // Arrange
        DateOnly saturday = new(2026, 10, 31);

        // Act
        Func<CreditTransfer> create = () => Arbitrary.CreditTransfer().WithRequestedExecutionDate(saturday).Generate();

        // Assert
        ArgumentException refusal = Assert.Throws<ArgumentException>(create);
        Assert.Equal("requestedExecutionDate", refusal.ParamName);
    }

    [Fact]
    public void Inside_SEPA_the_creditor_agent_may_be_omitted() {
        // Arrange
        Party anyCreditorWithoutAgent = Arbitrary.Party().WithAgent((Bic?)null).Generate();

        // Act
        CreditTransfer anyTransfer = Arbitrary.CreditTransfer().WithCreditor(anyCreditorWithoutAgent).Generate();

        // Assert
        Assert.Null(anyTransfer.Creditor.Agent);
    }

}
