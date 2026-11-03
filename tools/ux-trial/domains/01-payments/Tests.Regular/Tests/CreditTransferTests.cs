using JustDummies;

using Xunit;

namespace Payments.Domain.Tests;

public sealed class CreditTransferTests {

    private static readonly DateOnly AMonday = new DateOnly(2026, 3, 2);

    private static CreditTransfer NewTransfer(string?           endToEndId = "E2E-1",
                                              Party?            debtor     = null,
                                              Party?            creditor   = null,
                                              Money?            amount     = null,
                                              DateOnly?         date       = null,
                                              TransferPriority  priority   = TransferPriority.Normal,
                                              RemittanceInformation? remittance = null) {
        return new CreditTransfer(endToEndId!,
                                  debtor!,
                                  creditor!,
                                  amount!,
                                  date ?? AMonday,
                                  priority,
                                  remittance);
    }

    private static CreditTransfer ValidTransfer(string? endToEndId = null, Money? amount = null, DateOnly? date = null, TransferPriority priority = TransferPriority.Normal) {
        CreditTransfer anyTransfer = Any.CreditTransfer().Generate();

        return new CreditTransfer(endToEndId ?? anyTransfer.EndToEndId,
                                  anyTransfer.Debtor,
                                  anyTransfer.Creditor,
                                  amount ?? new Money(10m, Currency.EUR),
                                  date ?? anyTransfer.RequestedExecutionDate,
                                  priority,
                                  null);
    }

    private static CreditTransfer InState(TransferStatus status) {
        CreditTransfer transfer = Any.CreditTransfer().Generate();
        DateOnly       today    = transfer.RequestedExecutionDate;

        switch (status) {
            case TransferStatus.Draft:
                break;
            case TransferStatus.Submitted:
                transfer.Submit(today);
                break;
            case TransferStatus.Executed:
                transfer.Submit(today);
                transfer.MarkExecuted();
                break;
            case TransferStatus.Rejected:
                transfer.Submit(today);
                transfer.Reject();
                break;
        }

        return transfer;
    }

    // ---- Construction ---------------------------------------------------------------------

    [Fact]
    public void A_transfer_keeps_what_it_was_built_with_and_starts_as_a_draft() {
        // Arrange
        string                anyEndToEndId = Any.String().NonEmpty().WithMaxLength(35).Generate();
        Party                 anyDebtor     = Any.Party().Generate();
        Party                 anyCreditor   = Any.Party().Generate();
        Money                 anyAmount     = Any.Money().WithCurrency(Currency.EUR).Generate();
        RemittanceInformation anyReference  = Any.RemittanceInformation().Generate();

        // Act
        CreditTransfer transfer = new CreditTransfer(anyEndToEndId, anyDebtor, anyCreditor, anyAmount, AMonday, TransferPriority.Normal, anyReference);

        // Assert
        Assert.Equal(anyEndToEndId, transfer.EndToEndId);
        Assert.Same(anyDebtor, transfer.Debtor);
        Assert.Same(anyCreditor, transfer.Creditor);
        Assert.Same(anyAmount, transfer.Amount);
        Assert.Equal(AMonday, transfer.RequestedExecutionDate);
        Assert.Equal(TransferPriority.Normal, transfer.Priority);
        Assert.Same(anyReference, transfer.RemittanceInformation);
        Assert.Equal(TransferStatus.Draft, transfer.Status);
    }

    [Fact]
    public void The_remittance_information_is_optional() {
        // Arrange
        CreditTransfer anyTransfer = Any.CreditTransfer().WithoutRemittanceInformation().Generate();

        // Assert
        Assert.Null(anyTransfer.RemittanceInformation);
    }

    [Fact]
    public void A_drawn_transfer_is_always_valid_and_a_draft() {
        // Arrange & Act
        CreditTransfer anyTransfer = Any.CreditTransfer().Generate();

        // Assert
        Assert.Equal(TransferStatus.Draft, anyTransfer.Status);
        Assert.Equal(Currency.EUR, anyTransfer.Amount.Currency);
    }

    [Fact]
    public void A_null_end_to_end_id_is_refused_as_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => NewTransfer(endToEndId: null, debtor: Any.Party().Generate(), creditor: Any.Party().Generate(), amount: Any.Money().WithCurrency(Currency.EUR).Generate()));

        // Assert
        Assert.Equal("endToEndId", refusal.ParamName);
    }

    [Fact]
    public void An_empty_end_to_end_id_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => ValidTransfer(endToEndId: ""));

        // Assert
        Assert.Equal("endToEndId", refusal.ParamName);
        Assert.IsNotType<ArgumentNullException>(refusal);
    }

    [Fact]
    public void An_end_to_end_id_of_35_characters_is_accepted() {
        // Arrange
        string anyMaximalId = Any.String().WithLength(35).Generate();

        // Act
        CreditTransfer transfer = ValidTransfer(endToEndId: anyMaximalId);

        // Assert
        Assert.Equal(anyMaximalId, transfer.EndToEndId);
    }

    [Fact]
    public void An_end_to_end_id_of_36_characters_is_refused() {
        // Arrange
        string anyExcessiveId = Any.String().WithLength(36).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => ValidTransfer(endToEndId: anyExcessiveId));
    }

    [Fact]
    public void A_blank_end_to_end_id_is_accepted_because_only_null_and_empty_are_checked() {
        // Act
        CreditTransfer transfer = ValidTransfer(endToEndId: "   ");

        // Assert
        Assert.Equal("   ", transfer.EndToEndId);
    }

    [Fact]
    public void A_null_debtor_is_refused() {
        // Arrange
        Party anyCreditor = Any.Party().Generate();
        Money anyAmount   = Any.Money().WithCurrency(Currency.EUR).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => NewTransfer(creditor: anyCreditor, amount: anyAmount));

        // Assert
        Assert.Equal("debtor", refusal.ParamName);
    }

    [Fact]
    public void A_null_creditor_is_refused() {
        // Arrange
        Party anyDebtor = Any.Party().Generate();
        Money anyAmount = Any.Money().WithCurrency(Currency.EUR).Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => NewTransfer(debtor: anyDebtor, amount: anyAmount));

        // Assert
        Assert.Equal("creditor", refusal.ParamName);
    }

    [Fact]
    public void A_null_amount_is_refused() {
        // Arrange
        Party anyDebtor   = Any.Party().Generate();
        Party anyCreditor = Any.Party().Generate();

        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(() => NewTransfer(debtor: anyDebtor, creditor: anyCreditor));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void An_amount_in_another_currency_than_the_euro_is_refused() {
        // Arrange
        Currency anyForeignCurrency = Any.Enum<Currency>().DifferentFrom(Currency.EUR).Generate();
        Money    anyForeignAmount   = Any.Money().WithCurrency(anyForeignCurrency).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => ValidTransfer(amount: anyForeignAmount));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void A_debtor_and_a_creditor_on_the_same_account_are_refused_whoever_they_are() {
        // Arrange
        Iban  anyAccount  = Any.Iban().Generate();
        Party anyDebtor   = Any.Party().WithAccount(anyAccount).Generate();
        Party anyCreditor = Any.Party().WithAccount(anyAccount).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => NewTransfer(debtor: anyDebtor, creditor: anyCreditor, amount: new Money(10m, Currency.EUR)));

        // Assert
        Assert.Equal("creditor", refusal.ParamName);
    }

    [Fact]
    public void Two_accounts_of_equal_value_but_distinct_instances_are_the_same_account() {
        // Arrange
        string anyValue    = Any.Iban().Generate().Value;
        Party  anyDebtor   = Any.Party().WithAccount(new Iban(anyValue)).Generate();
        Party  anyCreditor = Any.Party().WithAccount(new Iban(anyValue)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => NewTransfer(debtor: anyDebtor, creditor: anyCreditor, amount: new Money(10m, Currency.EUR)));
    }

    [Theory]
    [InlineData(2026, 3, 7)]     // Saturday
    [InlineData(2026, 3, 8)]     // Sunday
    [InlineData(2026, 1, 1)]     // holiday
    [InlineData(2026, 12, 25)]   // holiday
    [InlineData(2026, 12, 26)]   // holiday
    public void A_requested_date_that_is_not_a_business_day_is_refused(int year, int month, int day) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => ValidTransfer(date: new DateOnly(year, month, day)));

        // Assert
        Assert.Equal("requestedExecutionDate", refusal.ParamName);
    }

    [Fact]
    public void An_instant_transfer_of_exactly_the_ceiling_is_accepted() {
        // Act
        CreditTransfer transfer = ValidTransfer(amount: new Money(CreditTransfer.InstantCeiling, Currency.EUR), priority: TransferPriority.Instant);

        // Assert
        Assert.Equal(TransferPriority.Instant, transfer.Priority);
    }

    [Fact]
    public void An_instant_transfer_above_the_ceiling_is_refused() {
        // Arrange
        decimal anyExcess = Any.Decimal().GreaterThan(CreditTransfer.InstantCeiling).LessThanOrEqualTo(Money.MaximumAmount).WithScale(2).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => ValidTransfer(amount: new Money(anyExcess, Currency.EUR), priority: TransferPriority.Instant));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void One_cent_above_the_ceiling_is_refused_for_an_instant_transfer_and_accepted_for_a_normal_one() {
        // Arrange
        Money aCentOver = new Money(100_000.01m, Currency.EUR);

        // Act
        CreditTransfer normal = ValidTransfer(amount: aCentOver, priority: TransferPriority.Normal);

        // Assert
        Assert.Equal(aCentOver, normal.Amount);
        Assert.Throws<ArgumentException>(() => ValidTransfer(amount: aCentOver, priority: TransferPriority.Instant));
    }

    [Fact]
    public void A_normal_transfer_may_reach_the_maximum_amount() {
        // Act
        CreditTransfer transfer = ValidTransfer(amount: new Money(Money.MaximumAmount, Currency.EUR));

        // Assert
        Assert.Equal(Money.MaximumAmount, transfer.Amount.Amount);
    }

    [Fact]
    public void A_priority_that_is_not_a_defined_value_is_not_checked() {
        // Arrange
        TransferPriority anyUndefinedPriority = (TransferPriority)Any.Int32().GreaterThan(10).Generate();

        // Act
        CreditTransfer transfer = ValidTransfer(amount: new Money(CreditTransfer.InstantCeiling + 1m, Currency.EUR), priority: anyUndefinedPriority);

        // Assert
        Assert.Equal(anyUndefinedPriority, transfer.Priority);
    }

    [Fact]
    public void The_checks_run_in_order_end_to_end_id_first() {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => NewTransfer(endToEndId: new string('x', 36), debtor: null, creditor: null, amount: null, date: new DateOnly(2026, 3, 7)));
    }

    [Fact]
    public void The_currency_is_checked_before_the_accounts() {
        // Arrange
        Iban  anyAccount = Any.Iban().Generate();
        Party anyDebtor   = Any.Party().WithAccount(anyAccount).Generate();
        Party anyCreditor = Any.Party().WithAccount(anyAccount).Generate();
        Money swiss       = new Money(10m, Currency.CHF);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => NewTransfer(debtor: anyDebtor, creditor: anyCreditor, amount: swiss));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void The_accounts_are_checked_before_the_date() {
        // Arrange
        Iban  anyAccount = Any.Iban().Generate();
        Party anyDebtor   = Any.Party().WithAccount(anyAccount).Generate();
        Party anyCreditor = Any.Party().WithAccount(anyAccount).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => NewTransfer(debtor: anyDebtor, creditor: anyCreditor, amount: new Money(10m, Currency.EUR), date: new DateOnly(2026, 3, 7)));

        // Assert
        Assert.Equal("creditor", refusal.ParamName);
    }

    [Fact]
    public void The_date_is_checked_before_the_instant_ceiling() {
        // Arrange
        Money tooBigForInstant = new Money(CreditTransfer.InstantCeiling + 1m, Currency.EUR);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => ValidTransfer(amount: tooBigForInstant, date: new DateOnly(2026, 3, 7), priority: TransferPriority.Instant));

        // Assert
        Assert.Equal("requestedExecutionDate", refusal.ParamName);
    }

    // ---- Cross-border -----------------------------------------------------------------------

    [Fact]
    public void Accounts_held_in_different_countries_make_a_cross_border_transfer() {
        // Arrange
        CreditTransfer anyCrossBorderTransfer = Any.CreditTransfer()
                                                   .WithDebtor(d => d.WithAccount(a => a.InCountry("DE")))
                                                   .WithCreditor(c => c.WithAccount(a => a.InCountry("FR")))
                                                   .Generate();

        // Assert
        Assert.True(anyCrossBorderTransfer.IsCrossBorder);
    }

    [Fact]
    public void Accounts_held_in_one_country_make_a_domestic_transfer() {
        // Arrange
        CreditTransfer anyDomesticTransfer = Any.CreditTransfer()
                                                .WithDebtor(d => d.WithAccount(a => a.InCountry("DE")))
                                                .WithCreditor(c => c.WithAccount(a => a.InCountry("DE")))
                                                .Generate();

        // Assert
        Assert.False(anyDomesticTransfer.IsCrossBorder);
    }

    // ---- Lifecycle --------------------------------------------------------------------------

    [Fact]
    public void A_draft_submitted_on_its_execution_date_is_submitted() {
        // Arrange
        CreditTransfer anyTransfer = Any.CreditTransfer().Generate();

        // Act
        anyTransfer.Submit(anyTransfer.RequestedExecutionDate);

        // Assert
        Assert.Equal(TransferStatus.Submitted, anyTransfer.Status);
    }

    [Fact]
    public void A_draft_submitted_before_its_execution_date_is_submitted() {
        // Arrange
        int            anyDaysAhead = Any.Int32().Between(1, 365).Generate();
        CreditTransfer anyTransfer  = Any.CreditTransfer().Generate();

        // Act
        anyTransfer.Submit(anyTransfer.RequestedExecutionDate.AddDays(-anyDaysAhead));

        // Assert
        Assert.Equal(TransferStatus.Submitted, anyTransfer.Status);
    }

    [Fact]
    public void A_draft_submitted_after_its_execution_date_is_refused_and_stays_a_draft() {
        // Arrange
        int            anyDaysLate = Any.Int32().Between(1, 365).Generate();
        CreditTransfer anyTransfer = Any.CreditTransfer().Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Submit(anyTransfer.RequestedExecutionDate.AddDays(anyDaysLate)));
        Assert.Equal(TransferStatus.Draft, anyTransfer.Status);
    }

    [Theory]
    [InlineData(TransferStatus.Submitted)]
    [InlineData(TransferStatus.Executed)]
    [InlineData(TransferStatus.Rejected)]
    public void Only_a_draft_can_be_submitted(TransferStatus status) {
        // Arrange
        CreditTransfer anyTransfer = InState(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Submit(anyTransfer.RequestedExecutionDate));
        Assert.Equal(status, anyTransfer.Status);
    }

    [Fact]
    public void A_submitted_transfer_can_be_executed() {
        // Arrange
        CreditTransfer anySubmittedTransfer = InState(TransferStatus.Submitted);

        // Act
        anySubmittedTransfer.MarkExecuted();

        // Assert
        Assert.Equal(TransferStatus.Executed, anySubmittedTransfer.Status);
    }

    [Fact]
    public void A_submitted_transfer_can_be_rejected() {
        // Arrange
        CreditTransfer anySubmittedTransfer = InState(TransferStatus.Submitted);

        // Act
        anySubmittedTransfer.Reject();

        // Assert
        Assert.Equal(TransferStatus.Rejected, anySubmittedTransfer.Status);
    }

    [Theory]
    [InlineData(TransferStatus.Draft)]
    [InlineData(TransferStatus.Executed)]
    [InlineData(TransferStatus.Rejected)]
    public void Only_a_submitted_transfer_can_be_executed(TransferStatus status) {
        // Arrange
        CreditTransfer anyTransfer = InState(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyTransfer.MarkExecuted());
        Assert.Equal(status, anyTransfer.Status);
    }

    [Theory]
    [InlineData(TransferStatus.Draft)]
    [InlineData(TransferStatus.Executed)]
    [InlineData(TransferStatus.Rejected)]
    public void Only_a_submitted_transfer_can_be_rejected(TransferStatus status) {
        // Arrange
        CreditTransfer anyTransfer = InState(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Reject());
        Assert.Equal(status, anyTransfer.Status);
    }

    [Fact]
    public void The_refusal_of_an_illegal_transition_names_the_current_status() {
        // Arrange
        CreditTransfer anyExecutedTransfer = InState(TransferStatus.Executed);

        // Act
        InvalidOperationException refusal = Assert.Throws<InvalidOperationException>(() => anyExecutedTransfer.Reject());

        // Assert
        Assert.Contains("Executed", refusal.Message);
    }

}
