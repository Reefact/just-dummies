using JustDummies;

using Payments.Domain;

using Xunit;

namespace Payments.Tests;

public sealed class CreditTransferTests {

    // Wednesday 4 March 2026, a plain TARGET2 business day.
    private static readonly DateOnly BusinessDay = new DateOnly(2026, 3, 4);

    private static CreditTransfer Build(string?                endToEndId             = null,
                                        Party?                 debtor                 = null,
                                        Party?                 creditor               = null,
                                        Money?                 amount                 = null,
                                        DateOnly?              requestedExecutionDate = null,
                                        TransferPriority       priority               = TransferPriority.Normal,
                                        RemittanceInformation? remittance             = null) {
        return new CreditTransfer(endToEndId ?? "E2E-1",
                                  debtor ?? Any.Party().Generate(),
                                  creditor ?? Any.Party().Generate(),
                                  amount ?? Any.Money().WithCurrency(Currency.EUR).Generate(),
                                  requestedExecutionDate ?? BusinessDay,
                                  priority,
                                  remittance);
    }

    private static CreditTransfer DraftOn(DateOnly executionDate) {
        return Any.CreditTransfer().WithRequestedExecutionDate(executionDate).Generate();
    }

    // --- construction ---------------------------------------------------------------------

    [Fact]
    public void A_new_transfer_keeps_its_data_and_starts_as_a_draft() {
        // Arrange
        Party                 anyDebtor   = Any.Party().Generate();
        Party                 anyCreditor = Any.Party().Generate();
        RemittanceInformation anyRemark   = Any.RemittanceInformation().Generate();

        // Act
        CreditTransfer transfer = new CreditTransfer("E2E-2026-0001", anyDebtor, anyCreditor, new Money(125.40m, Currency.EUR), BusinessDay, TransferPriority.Normal, anyRemark);

        // Assert
        Assert.Equal("E2E-2026-0001", transfer.EndToEndId);
        Assert.Same(anyDebtor, transfer.Debtor);
        Assert.Same(anyCreditor, transfer.Creditor);
        Assert.Equal(new Money(125.40m, Currency.EUR), transfer.Amount);
        Assert.Equal(BusinessDay, transfer.RequestedExecutionDate);
        Assert.Equal(TransferPriority.Normal, transfer.Priority);
        Assert.Same(anyRemark, transfer.RemittanceInformation);
        Assert.Equal(TransferStatus.Draft, transfer.Status);
    }

    [Fact]
    public void The_remittance_information_is_optional() {
        Assert.Null(Build(remittance: null).RemittanceInformation);
    }

    [Fact]
    public void An_end_to_end_id_of_35_characters_is_accepted() {
        // Arrange
        string anyId35 = Any.String().Alpha().WithLength(35).Generate();

        // Act & Assert
        Assert.Equal(anyId35, Build(endToEndId: anyId35).EndToEndId);
    }

    [Fact]
    public void An_end_to_end_id_of_36_characters_is_refused() {
        // Arrange
        string anyId36 = Any.String().Alpha().WithLength(36).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(endToEndId: anyId36));
    }

    [Fact]
    public void A_null_end_to_end_id_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CreditTransfer(null!, Any.Party().Generate(), Any.Party().Generate(), Any.Money().WithCurrency(Currency.EUR).Generate(), BusinessDay, TransferPriority.Normal, null));
    }

    [Fact]
    public void An_empty_end_to_end_id_is_refused() {
        Assert.Throws<ArgumentException>(() => Build(endToEndId: ""));
    }

    [Fact]
    public void A_null_debtor_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CreditTransfer("E2E-1", null!, Any.Party().Generate(), Any.Money().WithCurrency(Currency.EUR).Generate(), BusinessDay, TransferPriority.Normal, null));
    }

    [Fact]
    public void A_null_creditor_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CreditTransfer("E2E-1", Any.Party().Generate(), null!, Any.Money().WithCurrency(Currency.EUR).Generate(), BusinessDay, TransferPriority.Normal, null));
    }

    [Fact]
    public void A_null_amount_is_refused() {
        Assert.Throws<ArgumentNullException>(() => new CreditTransfer("E2E-1", Any.Party().Generate(), Any.Party().Generate(), null!, BusinessDay, TransferPriority.Normal, null));
    }

    [Fact]
    public void A_currency_other_than_the_euro_is_refused() {
        // Arrange
        Currency anyForeignCurrency = Any.Enum<Currency>().DifferentFrom(Currency.EUR).Generate();
        Money    anyForeignAmount   = Any.Money().WithCurrency(anyForeignCurrency).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(amount: anyForeignAmount));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void The_debtor_and_the_creditor_must_hold_different_accounts() {
        // Arrange
        Iban  anyAccount  = Any.Iban().Generate();
        Party anyDebtor   = Any.Party().WithAccount(anyAccount).Generate();
        Party anyCreditor = Any.Party().WithAccount(anyAccount).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(debtor: anyDebtor, creditor: anyCreditor));

        // Assert
        Assert.Equal("creditor", refusal.ParamName);
    }

    [Fact]
    public void The_same_account_is_recognised_by_value_not_by_instance() {
        // Arrange
        string anyValue    = Any.Iban().Generate().Value;
        Party  anyDebtor   = Any.Party().WithAccount(new Iban(anyValue)).Generate();
        Party  anyCreditor = Any.Party().WithAccount(new Iban(anyValue)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Build(debtor: anyDebtor, creditor: anyCreditor));
    }

    [Theory]
    [InlineData(2026, 3, 7)]   // Saturday
    [InlineData(2026, 3, 8)]   // Sunday
    [InlineData(2026, 12, 25)] // Christmas Day
    [InlineData(2026, 5, 1)]   // Labour Day
    public void A_date_which_is_not_a_business_day_is_refused(int year, int month, int day) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(requestedExecutionDate: new DateOnly(year, month, day)));

        // Assert
        Assert.Equal("requestedExecutionDate", refusal.ParamName);
    }

    [Fact]
    public void An_instant_transfer_of_exactly_the_ceiling_is_accepted() {
        // Arrange
        Money atTheCeiling = new Money(100_000m, Currency.EUR);

        // Act & Assert
        Assert.Equal(TransferPriority.Instant, Build(amount: atTheCeiling, priority: TransferPriority.Instant).Priority);
    }

    [Fact]
    public void An_instant_transfer_above_the_ceiling_is_refused() {
        // Arrange
        Money aCentAbove = new Money(100_000.01m, Currency.EUR);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(() => Build(amount: aCentAbove, priority: TransferPriority.Instant));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void A_standard_transfer_may_exceed_the_instant_ceiling() {
        // Arrange
        Money aCentAbove = new Money(100_000.01m, Currency.EUR);

        // Act & Assert
        Assert.Equal(aCentAbove, Build(amount: aCentAbove, priority: TransferPriority.Normal).Amount);
    }

    [Fact]
    public void The_end_to_end_id_length_is_checked_before_the_debtor() {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CreditTransfer(new string('X', 36), null!, null!, null!, BusinessDay, TransferPriority.Normal, null));
    }

    // --- cross-border ---------------------------------------------------------------------

    [Fact]
    public void Two_accounts_in_the_same_country_are_a_domestic_transfer() {
        // Arrange
        Party anyDebtor   = Any.Party().WithAccount(a => a.InCountry("FR")).Generate();
        Party anyCreditor = Any.Party().WithAccount(a => a.InCountry("FR")).Generate();

        // Act
        CreditTransfer transfer = Build(debtor: anyDebtor, creditor: anyCreditor);

        // Assert
        Assert.False(transfer.IsCrossBorder);
    }

    [Fact]
    public void Two_accounts_in_different_countries_are_a_cross_border_transfer() {
        // Arrange
        Party anyDebtor   = Any.Party().WithAccount(a => a.InCountry("FR")).Generate();
        Party anyCreditor = Any.Party().WithAccount(a => a.InCountry("DE")).Generate();

        // Act
        CreditTransfer transfer = Build(debtor: anyDebtor, creditor: anyCreditor);

        // Assert
        Assert.True(transfer.IsCrossBorder);
    }

    // --- life cycle -----------------------------------------------------------------------

    [Fact]
    public void A_draft_is_submitted_before_its_execution_date() {
        // Arrange
        CreditTransfer anyTransfer = DraftOn(BusinessDay);

        // Act
        anyTransfer.Submit(BusinessDay.AddDays(-1));

        // Assert
        Assert.Equal(TransferStatus.Submitted, anyTransfer.Status);
    }

    [Fact]
    public void A_draft_is_submitted_on_its_execution_date() {
        // Arrange
        CreditTransfer anyTransfer = DraftOn(BusinessDay);

        // Act
        anyTransfer.Submit(BusinessDay);

        // Assert
        Assert.Equal(TransferStatus.Submitted, anyTransfer.Status);
    }

    [Fact]
    public void A_draft_cannot_be_submitted_after_its_execution_date() {
        // Arrange
        CreditTransfer anyTransfer = DraftOn(BusinessDay);

        // Act
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Submit(BusinessDay.AddDays(1)));

        // Assert
        Assert.Equal(TransferStatus.Draft, anyTransfer.Status);
    }

    [Fact]
    public void A_submitted_transfer_cannot_be_submitted_again() {
        // Arrange
        CreditTransfer anyTransfer = DraftOn(BusinessDay);
        anyTransfer.Submit(BusinessDay);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Submit(BusinessDay));
    }

    [Fact]
    public void A_submitted_transfer_can_be_executed() {
        // Arrange
        CreditTransfer anyTransfer = DraftOn(BusinessDay);
        anyTransfer.Submit(BusinessDay);

        // Act
        anyTransfer.MarkExecuted();

        // Assert
        Assert.Equal(TransferStatus.Executed, anyTransfer.Status);
    }

    [Fact]
    public void A_submitted_transfer_can_be_rejected() {
        // Arrange
        CreditTransfer anyTransfer = DraftOn(BusinessDay);
        anyTransfer.Submit(BusinessDay);

        // Act
        anyTransfer.Reject();

        // Assert
        Assert.Equal(TransferStatus.Rejected, anyTransfer.Status);
    }

    [Fact]
    public void A_draft_can_be_neither_executed_nor_rejected() {
        // Arrange
        CreditTransfer anyTransfer = DraftOn(BusinessDay);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyTransfer.MarkExecuted());
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Reject());
        Assert.Equal(TransferStatus.Draft, anyTransfer.Status);
    }

    [Fact]
    public void An_executed_transfer_has_no_way_out() {
        // Arrange
        CreditTransfer anyTransfer = DraftOn(BusinessDay);
        anyTransfer.Submit(BusinessDay);
        anyTransfer.MarkExecuted();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Submit(BusinessDay));
        Assert.Throws<InvalidOperationException>(() => anyTransfer.MarkExecuted());
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Reject());
        Assert.Equal(TransferStatus.Executed, anyTransfer.Status);
    }

    [Fact]
    public void A_rejected_transfer_has_no_way_out() {
        // Arrange
        CreditTransfer anyTransfer = DraftOn(BusinessDay);
        anyTransfer.Submit(BusinessDay);
        anyTransfer.Reject();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Submit(BusinessDay));
        Assert.Throws<InvalidOperationException>(() => anyTransfer.MarkExecuted());
        Assert.Throws<InvalidOperationException>(() => anyTransfer.Reject());
        Assert.Equal(TransferStatus.Rejected, anyTransfer.Status);
    }

}
