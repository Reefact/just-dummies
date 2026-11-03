using System.Globalization;

using JustDummies;

using Xunit;

namespace Payments.Domain.Tests.Expert;

public sealed class CreditTransferTests {

    // A Tuesday, so a business day; the day before is a Monday.
    private static readonly DateOnly Tuesday = new DateOnly(2026, 3, 10);

    private static string AnyEndToEndId() => AnyCreditTransfer.ValidEndToEndIds().Generate();

    private static DateOnly AnyBusinessDay() => AnyCreditTransfer.ValidExecutionDates().Generate();

    // An amount any priority accepts: a standard transfer is not capped, an instant one is, and the test does not say which it is.
    private static Money AnyEuros() {
        return Any.Money().WithCurrency(Currency.EUR).WithAmount(a => a.LessThanOrEqualTo(CreditTransfer.InstantCeiling)).Generate();
    }

    private static Party AnyCreditor() => Any.Party().Generate();

    private static CreditTransfer AnyTransferIn(TransferStatus status) {
        CreditTransfer transfer = Any.CreditTransfer().WithRequestedExecutionDate(Tuesday).Generate();
        DateOnly       today    = Tuesday;
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

    // --- construction ------------------------------------------------------------------------------------------

    [Fact]
    public void A_transfer_keeps_what_it_was_built_with_and_starts_as_a_draft() {
        // Arrange
        Party                 debtor    = Any.Party().Generate();
        Party                 creditor  = AnyCreditor();
        Money                 amount    = AnyEuros();
        RemittanceInformation remittance = Any.RemittanceInformation().Generate();

        // Act
        CreditTransfer transfer = new CreditTransfer("E2E-0001", debtor, creditor, amount, Tuesday, TransferPriority.Normal, remittance);

        // Assert
        Assert.Equal("E2E-0001", transfer.EndToEndId);
        Assert.Same(debtor, transfer.Debtor);
        Assert.Same(creditor, transfer.Creditor);
        Assert.Same(amount, transfer.Amount);
        Assert.Equal(Tuesday, transfer.RequestedExecutionDate);
        Assert.Equal(TransferPriority.Normal, transfer.Priority);
        Assert.Same(remittance, transfer.RemittanceInformation);
        Assert.Equal(TransferStatus.Draft, transfer.Status);
    }

    [Fact]
    public void A_transfer_built_by_the_generator_is_a_draft() {
        // Act
        CreditTransfer anyTransfer = Any.CreditTransfer().Generate();

        // Assert
        Assert.Equal(TransferStatus.Draft, anyTransfer.Status);
    }

    [Fact]
    public void The_remittance_information_is_optional() {
        // Act
        CreditTransfer transfer = new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), AnyEuros(), AnyBusinessDay(), Any.Enum<TransferPriority>().Generate(), null);

        // Assert
        Assert.Null(transfer.RemittanceInformation);
    }

    // --- end-to-end id ----------------------------------------------------------------------------------------

    [Fact]
    public void A_null_end_to_end_id_is_refused_as_a_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new CreditTransfer(null!, Any.Party().Generate(), AnyCreditor(), AnyEuros(), AnyBusinessDay(), Any.Enum<TransferPriority>().Generate(), null));

        // Assert
        Assert.Equal("endToEndId", refusal.ParamName);
    }

    [Fact]
    public void An_empty_end_to_end_id_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CreditTransfer("", Any.Party().Generate(), AnyCreditor(), AnyEuros(), AnyBusinessDay(), Any.Enum<TransferPriority>().Generate(), null));

        // Assert
        Assert.Equal("endToEndId", refusal.ParamName);
    }

    [Fact]
    public void A_blank_end_to_end_id_is_not_refused_only_an_empty_one_is() {
        // Act
        CreditTransfer transfer = new CreditTransfer(" ", Any.Party().Generate(), AnyCreditor(), AnyEuros(), AnyBusinessDay(), Any.Enum<TransferPriority>().Generate(), null);

        // Assert
        Assert.Equal(" ", transfer.EndToEndId);
    }

    [Fact]
    public void An_end_to_end_id_of_35_characters_is_accepted() {
        // Arrange
        string anyMaximalId = Any.String().WithLength(35).Generate();

        // Act
        CreditTransfer transfer = new CreditTransfer(anyMaximalId, Any.Party().Generate(), AnyCreditor(), AnyEuros(), AnyBusinessDay(), Any.Enum<TransferPriority>().Generate(), null);

        // Assert
        Assert.Equal(35, transfer.EndToEndId.Length);
    }

    [Fact]
    public void An_end_to_end_id_of_36_characters_is_refused_as_out_of_range() {
        // Arrange
        string anyTooLongId = Any.String().WithLength(36).Generate();

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new CreditTransfer(anyTooLongId, Any.Party().Generate(), AnyCreditor(), AnyEuros(), AnyBusinessDay(), Any.Enum<TransferPriority>().Generate(), null));
    }

    // --- the parties and the amount ----------------------------------------------------------------------------

    [Fact]
    public void A_null_debtor_is_refused_as_a_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new CreditTransfer(AnyEndToEndId(), null!, AnyCreditor(), AnyEuros(), AnyBusinessDay(), Any.Enum<TransferPriority>().Generate(), null));

        // Assert
        Assert.Equal("debtor", refusal.ParamName);
    }

    [Fact]
    public void A_null_creditor_is_refused_as_a_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), null!, AnyEuros(), AnyBusinessDay(), Any.Enum<TransferPriority>().Generate(), null));

        // Assert
        Assert.Equal("creditor", refusal.ParamName);
    }

    [Fact]
    public void A_null_amount_is_refused_as_a_null() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), null!, AnyBusinessDay(), Any.Enum<TransferPriority>().Generate(), null));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void An_amount_in_another_currency_than_the_euro_is_refused() {
        // Arrange
        Currency anyForeignCurrency = Any.Enum<Currency>().DifferentFrom(Currency.EUR).Generate();
        Money    anyForeignAmount   = Any.Money().WithCurrency(anyForeignCurrency).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), anyForeignAmount, AnyBusinessDay(), TransferPriority.Normal, null));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void A_debtor_and_a_creditor_sharing_an_account_are_refused_whatever_else_they_share() {
        // Arrange
        Iban  sharedAccount = Any.Iban().Generate();
        Party debtor        = Any.Party().WithAccount(sharedAccount).Generate();
        Party creditor      = Any.Party().WithAccount(sharedAccount).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CreditTransfer(AnyEndToEndId(), debtor, creditor, AnyEuros(), AnyBusinessDay(), TransferPriority.Normal, null));

        // Assert
        Assert.Equal("creditor", refusal.ParamName);
    }

    [Fact]
    public void A_debtor_and_a_creditor_holding_equal_accounts_built_separately_are_refused() {
        // Arrange
        string anyAccountValue = Any.Iban().Generate().Value;
        Party  debtor          = Any.Party().WithAccount(new Iban(anyAccountValue)).Generate();
        Party  creditor        = Any.Party().WithAccount(new Iban(anyAccountValue)).Generate();

        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => new CreditTransfer(AnyEndToEndId(), debtor, creditor, AnyEuros(), AnyBusinessDay(), TransferPriority.Normal, null));
    }

    // --- the execution date ------------------------------------------------------------------------------------

    [Theory]
    [InlineData(2026, 3, 7)]     // Saturday
    [InlineData(2026, 3, 8)]     // Sunday
    [InlineData(2025, 1, 1)]     // New Year, a Wednesday
    [InlineData(2025, 5, 1)]     // Labour Day, a Thursday
    [InlineData(2025, 12, 25)]   // Christmas, a Thursday
    [InlineData(2025, 12, 26)]   // Boxing Day, a Friday
    public void A_requested_date_that_is_not_a_business_day_is_refused(int year, int month, int day) {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), AnyEuros(), new DateOnly(year, month, day), TransferPriority.Normal, null));

        // Assert
        Assert.Equal("requestedExecutionDate", refusal.ParamName);
    }

    [Theory]
    [InlineData(2026, 3, 9)]
    [InlineData(2025, 12, 24)]
    [InlineData(2025, 12, 31)]
    public void A_requested_date_that_is_a_business_day_is_accepted_and_kept(int year, int month, int day) {
        // Act
        CreditTransfer transfer = new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), AnyEuros(), new DateOnly(year, month, day), TransferPriority.Normal, null);

        // Assert
        Assert.Equal(new DateOnly(year, month, day), transfer.RequestedExecutionDate);
    }

    // --- the instant ceiling ----------------------------------------------------------------------------------

    [Fact]
    public void An_instant_transfer_of_exactly_the_ceiling_is_accepted() {
        // Act
        CreditTransfer transfer = new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), new Money(100_000m, Currency.EUR), AnyBusinessDay(), TransferPriority.Instant, null);

        // Assert
        Assert.Equal(TransferPriority.Instant, transfer.Priority);
    }

    [Fact]
    public void An_instant_transfer_one_cent_above_the_ceiling_is_refused() {
        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), new Money(100_000.01m, Currency.EUR), AnyBusinessDay(), TransferPriority.Instant, null));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void An_instant_transfer_of_the_largest_amount_is_refused() {
        // Act & Assert
        Assert.Throws<ArgumentException>(
            () => new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), new Money(Money.MaximumAmount, Currency.EUR), AnyBusinessDay(), TransferPriority.Instant, null));
    }

    [Theory]
    [InlineData("100000.01")]
    [InlineData("1000000")]
    [InlineData("999999999.99")]
    public void A_standard_transfer_is_not_capped(string amount) {
        // Arrange
        Money large = new Money(decimal.Parse(amount, CultureInfo.InvariantCulture), Currency.EUR);

        // Act
        CreditTransfer transfer = new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), large, AnyBusinessDay(), TransferPriority.Normal, null);

        // Assert
        Assert.Same(large, transfer.Amount);
    }

    [Fact]
    public void An_instant_transfer_below_the_ceiling_is_accepted() {
        // Act
        CreditTransfer transfer = new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), AnyEuros(), AnyBusinessDay(), TransferPriority.Instant, null);

        // Assert
        Assert.Equal(TransferPriority.Instant, transfer.Priority);
    }

    [Fact]
    public void A_priority_that_is_no_member_of_the_enum_is_not_refused_nor_capped() {
        // Act
        CreditTransfer transfer = new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), new Money(Money.MaximumAmount, Currency.EUR), AnyBusinessDay(), (TransferPriority)99, null);

        // Assert
        Assert.Equal((TransferPriority)99, transfer.Priority);
    }

    // --- the order in which the arguments are judged ----------------------------------------------------------

    [Fact]
    public void The_currency_is_judged_before_the_accounts() {
        // Arrange
        Iban  sharedAccount = Any.Iban().Generate();
        Party debtor        = Any.Party().WithAccount(sharedAccount).Generate();
        Party creditor      = Any.Party().WithAccount(sharedAccount).Generate();
        Money anyForeign    = Any.Money().WithCurrency(Any.Enum<Currency>().DifferentFrom(Currency.EUR).Generate()).Generate();

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CreditTransfer(AnyEndToEndId(), debtor, creditor, anyForeign, AnyBusinessDay(), TransferPriority.Normal, null));

        // Assert
        Assert.Equal("amount", refusal.ParamName);
    }

    [Fact]
    public void The_accounts_are_judged_before_the_date() {
        // Arrange
        Iban  sharedAccount = Any.Iban().Generate();
        Party debtor        = Any.Party().WithAccount(sharedAccount).Generate();
        Party creditor      = Any.Party().WithAccount(sharedAccount).Generate();
        DateOnly saturday   = new DateOnly(2026, 3, 7);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CreditTransfer(AnyEndToEndId(), debtor, creditor, AnyEuros(), saturday, TransferPriority.Normal, null));

        // Assert
        Assert.Equal("creditor", refusal.ParamName);
    }

    [Fact]
    public void The_date_is_judged_before_the_instant_ceiling() {
        // Arrange
        DateOnly saturday = new DateOnly(2026, 3, 7);
        Money    tooLargeForInstant = new Money(Money.MaximumAmount, Currency.EUR);

        // Act
        ArgumentException refusal = Assert.Throws<ArgumentException>(
            () => new CreditTransfer(AnyEndToEndId(), Any.Party().Generate(), AnyCreditor(), tooLargeForInstant, saturday, TransferPriority.Instant, null));

        // Assert
        Assert.Equal("requestedExecutionDate", refusal.ParamName);
    }

    [Fact]
    public void The_end_to_end_id_is_judged_before_everything_else() {
        // Act
        ArgumentNullException refusal = Assert.Throws<ArgumentNullException>(
            () => new CreditTransfer(null!, null!, null!, null!, new DateOnly(2026, 3, 7), TransferPriority.Instant, null));

        // Assert
        Assert.Equal("endToEndId", refusal.ParamName);
    }

    // --- cross-border -----------------------------------------------------------------------------------------

    [Fact]
    public void A_transfer_between_accounts_of_two_countries_is_cross_border() {
        // Arrange
        CreditTransfer transfer = Any.CreditTransfer()
                                     .WithDebtor(d => d.WithAccount(a => a.InCountry("FR")))
                                     .WithCreditor(c => c.WithAccount(a => a.InCountry("DE")))
                                     .Generate();

        // Act
        bool isCrossBorder = transfer.IsCrossBorder;

        // Assert
        Assert.True(isCrossBorder);
    }

    [Fact]
    public void A_transfer_between_two_accounts_of_one_country_is_not_cross_border() {
        // Arrange
        CreditTransfer transfer = Any.CreditTransfer()
                                     .WithDebtor(d => d.WithAccount(a => a.InCountry("FR")))
                                     .WithCreditor(c => c.WithAccount(a => a.InCountry("FR")))
                                     .Generate();

        // Act
        bool isCrossBorder = transfer.IsCrossBorder;

        // Assert
        Assert.False(isCrossBorder);
    }

    [Fact]
    public void The_agents_country_does_not_decide_whether_a_transfer_is_cross_border() {
        // Arrange
        CreditTransfer transfer = Any.CreditTransfer()
                                     .WithDebtor(d => d.WithAccount(a => a.InCountry("FR")))
                                     .WithCreditor(c => c.WithAccount(a => a.InCountry("FR")).WithoutAgent())
                                     .Generate();

        // Act
        bool isCrossBorder = transfer.IsCrossBorder;

        // Assert
        Assert.False(isCrossBorder);
    }

    // --- the lifecycle ----------------------------------------------------------------------------------------

    [Fact]
    public void A_draft_is_submitted_on_the_day_it_is_requested() {
        // Arrange
        CreditTransfer transfer = Any.CreditTransfer().WithRequestedExecutionDate(Tuesday).Generate();

        // Act
        transfer.Submit(Tuesday);

        // Assert
        Assert.Equal(TransferStatus.Submitted, transfer.Status);
    }

    [Fact]
    public void A_draft_is_submitted_before_the_day_it_is_requested() {
        // Arrange
        CreditTransfer transfer = Any.CreditTransfer().WithRequestedExecutionDate(Tuesday).Generate();

        // Act
        transfer.Submit(Tuesday.AddDays(-1));

        // Assert
        Assert.Equal(TransferStatus.Submitted, transfer.Status);
    }

    [Fact]
    public void A_draft_is_submitted_long_before_the_day_it_is_requested() {
        // Arrange
        CreditTransfer transfer = Any.CreditTransfer().WithRequestedExecutionDate(Tuesday).Generate();

        // Act
        transfer.Submit(Tuesday.AddYears(-1));

        // Assert
        Assert.Equal(TransferStatus.Submitted, transfer.Status);
    }

    [Fact]
    public void A_draft_cannot_be_submitted_the_day_after_the_day_it_is_requested() {
        // Arrange
        CreditTransfer transfer = Any.CreditTransfer().WithRequestedExecutionDate(Tuesday).Generate();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transfer.Submit(Tuesday.AddDays(1)));
    }

    [Fact]
    public void A_draft_refused_for_a_past_date_stays_a_draft() {
        // Arrange
        CreditTransfer transfer = Any.CreditTransfer().WithRequestedExecutionDate(Tuesday).Generate();

        // Act
        Assert.Throws<InvalidOperationException>(() => transfer.Submit(Tuesday.AddDays(1)));

        // Assert
        Assert.Equal(TransferStatus.Draft, transfer.Status);
    }

    [Theory]
    [InlineData(TransferStatus.Submitted)]
    [InlineData(TransferStatus.Executed)]
    [InlineData(TransferStatus.Rejected)]
    public void Only_a_draft_can_be_submitted(TransferStatus status) {
        // Arrange
        CreditTransfer transfer = AnyTransferIn(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transfer.Submit(Tuesday));
        Assert.Equal(status, transfer.Status);
    }

    [Fact]
    public void A_submitted_transfer_is_executed() {
        // Arrange
        CreditTransfer transfer = AnyTransferIn(TransferStatus.Submitted);

        // Act
        transfer.MarkExecuted();

        // Assert
        Assert.Equal(TransferStatus.Executed, transfer.Status);
    }

    [Theory]
    [InlineData(TransferStatus.Draft)]
    [InlineData(TransferStatus.Executed)]
    [InlineData(TransferStatus.Rejected)]
    public void Only_a_submitted_transfer_can_be_executed(TransferStatus status) {
        // Arrange
        CreditTransfer transfer = AnyTransferIn(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transfer.MarkExecuted());
        Assert.Equal(status, transfer.Status);
    }

    [Fact]
    public void A_submitted_transfer_is_rejected() {
        // Arrange
        CreditTransfer transfer = AnyTransferIn(TransferStatus.Submitted);

        // Act
        transfer.Reject();

        // Assert
        Assert.Equal(TransferStatus.Rejected, transfer.Status);
    }

    [Theory]
    [InlineData(TransferStatus.Draft)]
    [InlineData(TransferStatus.Executed)]
    [InlineData(TransferStatus.Rejected)]
    public void Only_a_submitted_transfer_can_be_rejected(TransferStatus status) {
        // Arrange
        CreditTransfer transfer = AnyTransferIn(status);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => transfer.Reject());
        Assert.Equal(status, transfer.Status);
    }

}
